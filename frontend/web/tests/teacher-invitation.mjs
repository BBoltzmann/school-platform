import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import vm from "node:vm";
import ts from "typescript";
import { NextResponse } from "next/server.js";
const root = new URL("../src/", import.meta.url);
test("teacher invitation activation and admin controls are wired", () => {
  const activation = fs.readFileSync(new URL("app/teacher/activate/page.tsx", root), "utf8");
  const staff = fs.readFileSync(new URL("components/staff/teacher-invite-actions.tsx", root), "utf8");
  const detail = fs.readFileSync(new URL("app/app/[tenantSlug]/(portal)/staff/[id]/page.tsx", root), "utf8");
  assert.match(activation, /Activate Your Teacher Account/);
  assert.match(activation, /readOnly=\{Boolean\(preview\?\.email\)\}/);
  assert.match(activation, /teacher-invite\/activate/);
  assert.match(staff, /teacher-invite/);
  assert.match(staff, /Copy activation link/);
  assert.match(staff, /Teacher Portal Access/);
  assert.match(staff, /status could not be loaded/);
  assert.match(detail, /TeacherInviteActions/);
});

function load(file, imports, fetch) {
  const exports = {};
  vm.runInNewContext(ts.transpileModule(fs.readFileSync(file, "utf8"), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText, { exports, require: name => imports[name], fetch });
  return exports;
}
const recovery = load("src/lib/auth/recovery.ts", {});
function activate(fetch) {
  return load("src/app/api/auth/teacher-invite/activate/route.ts", {
    "next/server": { NextResponse },
    "@/lib/api/backend-url": { getBackendUrl: () => "https://api.example" },
    "@/lib/auth/recovery": recovery,
  }, fetch).POST;
}
const activationRequest = password => new Request("https://school.example/api/auth/teacher-invite/activate", {
  method: "POST", headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ token: "A".repeat(64), email: "teacher@example.test", password }),
});
test("teacher activation explains password rejection before calling the backend", async () => {
  const post = activate(() => assert.fail("Invalid passwords must not reach the backend"));
  for (const password of ["Short12345", "letterswithoutdigits", "123456789012", "A1" + "x".repeat(127)]) {
    const response = await post(activationRequest(password));
    assert.equal(response.status, 400);
    assert.deepEqual(await response.json(), { error: recovery.PASSWORD_DESCRIPTION, code: "password_policy" });
  }
});
test("teacher activation forwards the same token and accepts a compliant password", async () => {
  const post = activate(async (url, options) => {
    assert.equal(url, "https://api.example/api/auth/teacher-invite/activate");
    assert.deepEqual(JSON.parse(options.body), {
      token: "A".repeat(64), email: "teacher@example.test", password: "Teacher-password-123",
    });
    return Response.json({ tenantSlug: "antioch-college" });
  });
  const response = await post(activationRequest("Teacher-password-123"));
  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), { tenantSlug: "antioch-college" });
});
