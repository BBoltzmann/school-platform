import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import * as jsxRuntime from 'react/jsx-runtime';
import { test } from 'node:test';
import assert from 'node:assert/strict';
import ts from 'typescript';
import fs from 'node:fs';
import vm from 'node:vm';
import { NextResponse } from 'next/server.js';

function load(file, env = {}, fetch, imports = {}) {
  const exports = {};
  vm.runInNewContext(ts.transpileModule(fs.readFileSync(file, 'utf8'), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX },
  }).outputText, {
    exports, require: name => imports[name], process: { env },
    URL, URLSearchParams, Date, AbortSignal, fetch, console: { error() {} },
  });
  return exports;
}
const recovery = load('src/lib/auth/recovery.ts');
const origin = 'https://api.example';
function handler(env = {}, fetch = async () => Response.json({})) {
  const configuration = { SCHOOL_PLATFORM_API_URL: origin, ...env };
  return load('src/lib/api/public-auth.ts', configuration, fetch, {
    'next/server': { NextResponse },
    '@/lib/api/backend-url': load('src/lib/api/backend-url.ts', configuration),
    '@/lib/auth/recovery': recovery,
  }).publicAuthPost;
}
const request = (body = {}, origin = 'https://frontend.example') => new Request('https://frontend.example/api/auth/forgot-password', {
  method: 'POST', headers: { origin, 'Content-Type': 'application/json' }, body: JSON.stringify(body),
});

test('password policy accepts passphrases with numbers and enforces length', () => {
  for (const password of ['short123', 'letterswithoutdigits', '123456789012', 'A1' + 'x'.repeat(127)]) assert.equal(recovery.validPassword(password), false);
  assert.equal(recovery.validPassword('A long passphrase 123'), true);
});

test('successful recovery never returns account or provider details', async () => {
  const post = handler({}, async (url, options) => {
    assert.equal(url, `${origin}/api/auth/forgot-password`);
    assert.equal(JSON.parse(options.body).tenantSlug, 'antioch-college');
    return Response.json({ accountExists: true, privateDetail: 'not-for-browser' });
  });
  const response = await post(request({ email: 'admin@antiochcollege.local', tenantSlug: 'antioch-college' }), 'forgot-password');
  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), { message: recovery.RECOVERY_MESSAGE });
});

test('reset forwards token only to backend and clears old session on success', async () => {
  const post = handler({ NODE_ENV: 'production' }, async (url, options) => {
    assert.equal(url, `${origin}/api/auth/reset-password`);
    assert.deepEqual(JSON.parse(options.body), { token: 'test-token', newPassword: 'New password 123' });
    return Response.json({ tenantSlug: 'new-school', accessToken: 'must-not-be-forwarded' });
  });
  const response = await post(request({ token: 'test-token', newPassword: 'New password 123' }), 'reset-password');
  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), { tenantSlug: 'new-school' });
  assert.match(response.headers.get('set-cookie'), /Max-Age=0/);
  assert.match(response.headers.get('set-cookie'), /HttpOnly/);
  assert.equal(recovery.loginDestination('new-school', true), '/login?tenantSlug=new-school&reset=success');
});

test('signup is gated and uses a generic response when enabled', async () => {
  const unavailable = await handler({}, () => assert.fail('must not fetch'))(request(), 'signup');
  assert.equal(unavailable.status, 404);
  const enabled = await handler({ ALLOW_PUBLIC_SCHOOL_SIGNUP: 'true' })(request(), 'signup');
  assert.deepEqual(await enabled.json(), { message: recovery.SIGNUP_MESSAGE });
});

test('upstream errors, missing configuration, and network failures are sanitized', async () => {
  for (const status of [400, 404, 409, 429, 500, 503]) {
    const response = await handler({ ALLOW_PUBLIC_SCHOOL_SIGNUP: 'true' }, async () => new Response('private backend exception', { status }))(request(), 'signup');
    assert.equal(response.status, status >= 500 ? 502 : status);
    assert.ok(!(await response.text()).includes('private backend exception'));
  }
  const network = await handler({}, async () => { throw new Error('private transport failure'); })(request(), 'forgot-password');
  assert.equal(network.status, 502);
  const config = await handler({ NODE_ENV: 'production', SCHOOL_PLATFORM_API_URL: '' })(request(), 'forgot-password');
  assert.equal(config.status, 503);
});

test('rejects malformed requests and cross-origin submissions before contacting backend', async () => {
  const post = handler({}, () => assert.fail('must not fetch'));
  assert.equal((await post(request({}, 'https://attacker.example'), 'forgot-password')).status, 403);
  assert.equal((await post(request(null), 'reset-password')).status, 400);
  const malformed = new Request('https://frontend.example/api/auth/reset-password', { method: 'POST', body: '{' });
  assert.equal((await post(malformed, 'reset-password')).status, 400);
});

test('rejects malformed successful reset responses', async () => {
  const response = await handler({}, async () => Response.json({ tenantSlug: '//attacker.example' }))(request(), 'reset-password');
  assert.equal(response.status, 502);
  assert.equal(response.headers.get('set-cookie'), null);
});

test('direct reset requires a code and matching strong passwords before submission', () => {
  const data = new FormData();
  data.set('email', ' admin@example.com ');
  data.set('tenantSlug', ' ANTIOCH-COLLEGE ');
  data.set('newPassword', 'Replacement password 123');
  data.set('confirmPassword', 'Replacement password 123');
  assert.ok(recovery.directResetPayload(data).error);
  data.set('recoveryCode', 'operator-entered-test-value');
  data.set('confirmPassword', 'different password 123');
  assert.equal(recovery.directResetPayload(data).error, 'Passwords do not match.');
  data.set('confirmPassword', 'Replacement password 123');
  const payload = recovery.directResetPayload(data);
  assert.equal(payload.body.email, 'admin@example.com');
  assert.equal(payload.body.tenantSlug, 'antioch-college');
  assert.equal(payload.body.recoveryCode, data.get('recoveryCode'));
  assert.equal(payload.body.newPassword, data.get('newPassword'));
  assert.equal(payload.body.confirmPassword, undefined);
});

test('direct-reset BFF forwards user input to the correct endpoint and clears old sessions', async () => {
  const body = { email: 'admin@example.com', tenantSlug: 'antioch-college', recoveryCode: 'operator-entered-test-value', newPassword: 'Replacement password 123' };
  const post = handler({ NODE_ENV: 'production' }, async (url, options) => {
    assert.equal(url, `${origin}/api/auth/direct-password-reset`);
    assert.deepEqual(JSON.parse(options.body), body);
    return Response.json({ tenantSlug: 'antioch-college' });
  });
  const response = await post(request(body), 'direct-password-reset');
  assert.equal(response.status, 200);
  assert.match(response.headers.get('set-cookie'), /Max-Age=0/);
  assert.match(response.headers.get('set-cookie'), /Secure/);
  assert.deepEqual(await response.json(), { tenantSlug: 'antioch-college' });
});

test('direct reset reports disabled, rejected, rate-limited and service failures without backend details', async () => {
  for (const status of [400, 404, 429, 500, 503]) {
    const response = await handler({}, async () => new Response('private server detail', { status }))(request(), 'direct-password-reset');
    assert.equal(response.status, status >= 500 ? 502 : status);
    const result = await response.json();
    assert.ok(!result.error.includes('private server detail'));
    if (status === 404) assert.match(result.error, /currently unavailable/);
    if (status === 400) assert.match(result.error, /recovery details/);
  }
});


test('direct-reset form prefills Antioch and leaves the recovery-code field empty', () => {
  const { DirectRecoveryForm } = load('src/components/auth/direct-recovery-form.tsx', {}, undefined, {
    react: React,
    'react/jsx-runtime': jsxRuntime,
    'next/navigation': { useRouter: () => ({ replace() {} }) },
    '@/components/ui/input': { Input: props => React.createElement('input', props) },
    '@/components/ui/button': { Button: props => React.createElement('button', props) },
    '@/lib/auth/recovery': recovery,
  });
  const html = renderToStaticMarkup(React.createElement(DirectRecoveryForm, {}));
  assert.match(html, /value="antiochcollege41@gmail.com"/);
  assert.match(html, /value="antioch-college"/);
  const codeField = html.match(/<input[^>]*name="recoveryCode"[^>]*>/)?.[0];
  assert.ok(codeField);
  assert.match(codeField, /type="password"/);
  assert.ok(!codeField.includes('value='));
  assert.match(html, /name="newPassword"/);
  assert.match(html, /name="confirmPassword"/);
  assert.match(html, /Change password/);
});
