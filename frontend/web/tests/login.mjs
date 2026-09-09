import { test } from 'node:test';
import assert from 'node:assert/strict';
import ts from 'typescript';
import fs from 'node:fs';
import vm from 'node:vm';
import { NextResponse } from 'next/server.js';

function load(file, env, fetch, imports = {}) {
  const exports = {};
  vm.runInNewContext(ts.transpileModule(fs.readFileSync(file, 'utf8'), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText, {
    exports, require: name => imports[name], process: { env },
    URL, Date, AbortSignal, fetch, console: { error() {} },
  });
  return exports;
}

const origin = 'https://school-platform-production-09fa.up.railway.app';
const credentials = { tenantSlug: 'antioch-college', email: 'admin@antiochcollege.local', password: 'test-only' };
function handler(env, fetch) {
  const config = load('src/lib/api/backend-url.ts', env);
  return load('src/app/api/auth/login/route.ts', env, fetch, {
    'next/server': { NextResponse }, '@/lib/api/backend-url': config,
  }).POST;
}
const request = () => new Request('https://frontend.example/api/auth/login', {
  method: 'POST', body: JSON.stringify(credentials),
});

test('normalizes origin and forwards exact credentials; stores secure cookie without exposing token', async () => {
  const post = handler({ NODE_ENV: 'production', SCHOOL_PLATFORM_API_URL: ` ${origin}/\n` }, async (url, options) => {
    assert.equal(url, `${origin}/api/auth/login`);
    assert.deepEqual(JSON.parse(options.body), credentials);
    assert.equal(options.cache, 'no-store');
    return Response.json({ accessToken: 'test-token', expiresAtUtc: new Date(Date.now() + 60000).toISOString() });
  });
  const response = await post(request());
  assert.equal(response.status, 200);
  const cookie = response.headers.get('set-cookie');
  for (const attribute of ['school_platform_token=test-token', 'Path=/', 'HttpOnly', 'Secure', 'SameSite=lax', 'Expires=']) assert.ok(cookie.includes(attribute), attribute);
  assert.equal((await response.json()).accessToken, undefined);
});

test('requires production configuration and rejects API paths', async () => {
  for (const value of [undefined, '', `${origin}/api`]) {
    const response = await handler({ NODE_ENV: 'production', SCHOOL_PLATFORM_API_URL: value }, () => assert.fail('must not fetch'))(request());
    assert.equal(response.status, 503);
  }
});

test('distinguishes rejected credentials from routing, server, network, and invalid-response failures', async () => {
  for (const status of [401, 404, 500, 503]) {
    const response = await handler({ SCHOOL_PLATFORM_API_URL: origin }, async () => new Response('', { status }))(request());
    assert.equal(response.status, status === 401 ? 401 : 502);
    assert.equal((await response.json()).error.includes('Invalid email'), status === 401);
    assert.equal(response.headers.get('set-cookie'), null);
  }
  for (const fetch of [async () => { throw new Error('network'); }, async () => Response.json({}), async () => new Response('<html>')]) {
    const response = await handler({ SCHOOL_PLATFORM_API_URL: origin }, fetch)(request());
    assert.equal(response.status, 502);
    assert.equal(response.headers.get('set-cookie'), null);
  }
});
