import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import vm from "node:vm";
import ts from "typescript";

const root = new URL("../src/", import.meta.url);
const read = path => fs.readFileSync(new URL(path, root), "utf8");

const exports = {};
vm.runInNewContext(ts.transpileModule(read("lib/portal-routes.ts"), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
}).outputText, { exports, require: () => assert.fail("No external dependencies expected"), URLSearchParams, encodeURIComponent });
const { schoolHomeForRoles } = exports;

test("school destination depends on authenticated roles, not login link", () => {
  assert.equal(schoolHomeForRoles("antioch-college", ["Teacher"]), "/app/antioch-college/teacher");
  assert.equal(schoolHomeForRoles("antioch-college", ["Administrator"]), "/app/antioch-college/dashboard");
  assert.equal(schoolHomeForRoles("antioch-college", ["Teacher", "Administrator"]), "/app/antioch-college/dashboard");
  assert.equal(schoolHomeForRoles("school two", ["Teacher"]), "/app/school%20two/teacher");
  assert.equal(schoolHomeForRoles("antioch-college", []), null);
});

test("login entry never hijacks the requested portal due to existing teacher session", () => {
  const login = read("components/auth/school-portal-login.tsx");
  assert.doesNotMatch(login, /getSessionContext|redirect\(/);
  assert.match(login, /tenantLocked/);
});

test("login success uses role-derived destination, not selected portal", () => {
  const login = read("components/auth/login-form.tsx");
  assert.match(login, /schoolHomeForRoles\(/);
  assert.match(login, /window\.location\.replace\(destination\)/);
  assert.doesNotMatch(login, /router\.push\(isTeacherOnly/);
});

test("teacher dashboard content uses client rendering for its event handlers", () => {
  const view = read("components/teacher/teacher-portal-view.tsx");
  assert.match(view, /^"use client";/);
  assert.match(view, /onClick=\{printTeacherTimetable\}/);
});

test("cross-tenant sessions do not automatically redirect to the old tenant dashboard", () => {
  const layout = read("app/app/[tenantSlug]/(portal)/layout.tsx");
  assert.match(layout, /redirect\(`\/schools\/\$\{encodeURIComponent\(tenantSlug\)\}`\)/);
  assert.doesNotMatch(layout, /`\/app\/\$\{session\.tenantSlug\}\/dashboard`/);
});
