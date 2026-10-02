import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const root = new URL("../src/", import.meta.url);
const read = (file) => fs.readFileSync(new URL(file, root), "utf8");

test("class subject management uses real subjects and persists selections", () => {
  const source = read("components/academics/class-subject-actions.tsx");
  assert.match(source, /Subjects Offered/);
  assert.match(source, /subjectIds/);
  assert.match(source, /\/subjects/);
  assert.match(read("app/app/[tenantSlug]/academics/page.tsx"), /ClassSubjectActions/);
  assert.match(read("components/staff/teaching-assignments-card.tsx"), /usesCustomSubjectOffering/);
});

test("subjects are saved through an authenticated BFF route", () => {
  const route = read("app/api/academics/classes/[id]/subjects/route.ts");
  assert.match(route, /authenticatedBackendFetch/);
  assert.match(route, /export async function PUT/);
  assert.match(route, /export async function POST/);
  assert.match(read("components/academics/class-subject-actions.tsx"), /method: "PUT"/);
  assert.match(read("components/academics/class-subject-actions.tsx"), /Reset Selected Subjects/);
  assert.match(read("components/academics/class-subject-actions.tsx"), /preserving existing teacher assignments/);
  assert.match(read("components/academics/class-subject-actions.tsx"), /academicSessionId=\$\{encodeURIComponent\(academicSessionId\)\}/);
  assert.match(read("components/academics/class-subject-actions.tsx"), /window\.confirm/);
});

test("parallel groups use persisted ClassSubject ids and controlled checkboxes", () => {
  const source = read("components/academics/class-subject-actions.tsx");
  assert.match(source, /persistedSubjects/);
  assert.match(source, /checked=\{groupSelected\.includes\(subject\.id\)\}/);
  assert.match(source, /setGroupSelected/);
  assert.match(source, /Save Subjects Offered before configuring a parallel group/);
});

test("fee structures expose authenticated student assignment workflow", () => {
  const workspace = read("components/fees/fee-structures-workspace.tsx");
  const route = read("app/api/fees/structures/[structureId]/students/route.ts");
  const removeRoute = read("app/api/fees/structures/[structureId]/students/[studentId]/route.ts");

  assert.match(workspace, /Manage Students/);
  assert.match(workspace, /Save Student Assignments/);
  assert.match(workspace, /studentSearch/);
  assert.match(workspace, /studentClassFilter/);
  assert.match(workspace, /studentIds/);
  assert.match(route, /authenticatedBackendFetch/);
  assert.match(route, /export async function GET/);
  assert.match(route, /export async function PUT/);
  assert.match(removeRoute, /export async function DELETE/);
});

test("fee structure management exposes editable templates and safe generation review", () => {
  const workspace = read("components/fees/fee-structures-workspace.tsx");
  const structureRoute = read("app/api/fees/structures/[structureId]/route.ts");

  assert.match(workspace, /Manage Structure/);
  assert.match(workspace, /Fee Components/);
  assert.match(workspace, /Save Structure Changes/);
  assert.match(workspace, /Generate \/ sync fees for/);
  assert.match(workspace, /paid charges and payment history are preserved/);
  assert.match(structureRoute, /export async function PUT/);
});

test("fee management exposes assigned-student removal and account filters", () => {
  const structures = read("components/fees/fee-structures-workspace.tsx");
  const accounts = read("components/fees/student-accounts-workspace.tsx");
  const outstanding = read("components/fees/outstanding-fees-workspace.tsx");

  assert.match(structures, /Assigned Students/);
  assert.match(structures, /method: "DELETE"/);
  assert.match(structures, /Run Generate \/ Sync Fees to reconcile unpaid generated charges/);
  assert.match(structures, /Remove \$\{student\.studentName\}/);
  assert.match(accounts, /All classes/);
  assert.match(accounts, /classFilter/);
  assert.match(outstanding, /Search student/);
  assert.match(outstanding, /All classes/);
  assert.match(outstanding, /row\.className/);
});

test("fee structures expose term reconciliation", () => {
  assert.match(read("components/fees/fee-structures-workspace.tsx"), /Reconcile Term Fees/);
  assert.match(read("app/api/fees/terms/[academicTermId]/reconcile/route.ts"), /authenticatedBackendFetch/);
  assert.match(read("app/api/fees/terms/[academicTermId]/reconcile/route.ts"), /method: "POST"/);
});

test("parallel group management and effective timetable capacity are visible", () => {
  const groups = read("components/academics/class-subject-actions.tsx");
  const requirements = read("components/timetable/class-subject-requirements-editor.tsx");
  const resetRoute = read("app/api/academics/classes/[id]/parallel-subject-groups/reset/route.ts");

  assert.match(groups, /Configured groups/);
  assert.match(groups, /Reset Parallel Groups/);
  assert.match(groups, /Delete/);
  assert.match(groups, /Retry/);
  assert.match(groups, /periods\/week/);
  assert.match(groups, /Not configured/);
  assert.match(groups, /saveRequirement/);
  assert.match(groups, /Parallel group:/);
  assert.match(groups, /disabled=\{resettingGroups \|\| !academicSessionId\}/);
  assert.match(resetRoute, /authenticatedBackendFetch/);
  assert.match(requirements, /Parallel savings/);
  assert.match(requirements, /Timetable periods required/);
});
