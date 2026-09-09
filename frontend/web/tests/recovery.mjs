import { test } from 'node:test';
import assert from 'node:assert/strict';
import ts from 'typescript';
import fs from 'node:fs';
import vm from 'node:vm';
import { NextResponse } from 'next/server.js';

function load(file, env = {}, fetch, imports = {}) {
  const exports = {};
  vm.runInNewContext(ts.transpileModule(fs.readFileSync(file, 'utf8'), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
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
