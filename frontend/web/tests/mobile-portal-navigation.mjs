import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const root = new URL("../src/", import.meta.url);
const source = path => fs.readFileSync(new URL(path, root), "utf8");

test("mobile header exposes an accessible working drawer trigger", () => {
  const header = source("components/layout/admin-header.tsx");
  assert.match(header, /<Sheet open=\{mobileMenuOpen\}/);
  assert.match(header, /<SheetTrigger[\s\S]*?aria-label="Open navigation"/);
  assert.match(header, /lg:hidden/);
  assert.match(header, /<SheetClose[\s\S]*?aria-label="Close navigation"/);
  assert.match(header, /<SidebarNavigation[\s\S]*?onNavigate=\{\(\) => setMobileMenuOpen\(false\)\}/);
});

test("desktop and mobile share the same permission-aware navigation", () => {
  const nav = source("components/navigation/admin-sidebar.tsx");
  const shell = source("components/layout/admin-shell.tsx");
  const teacher = source("components/navigation/teacher-navigation.ts");
  assert.match(nav, /export function SidebarNavigation/);
  assert.match(nav, /roles\.includes\("Teacher"\) \? teacherNavigation : adminNavigation/);
  assert.match(nav, /<SidebarNavigation tenantSlug=\{tenantSlug\} roles=\{roles\}/);
  assert.match(shell, /<AdminHeader[\s\S]*?roles=\{roles\}/);
  assert.match(teacher, /My Assessments/);
});

test("phone score entry uses the existing assessment save operation", () => {
  const page = source("components/assessments/assessment-workspace.tsx");
  assert.match(page, /md:hidden/);
  assert.match(page, /mobile-score-\$\{student\.studentId\}/);
  assert.match(page, /onChange=\{\(event\) => updateScore\(student.studentId, event.target.value\)\}/);
  assert.match(page, /<div className="hidden overflow-x-auto md:block">/);
  assert.match(page, /onClick=\{saveScores\}/);
});
