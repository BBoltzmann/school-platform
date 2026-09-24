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
  assert.match(workspace, /Generate charges for/);
  assert.match(workspace, /Existing charges and payments remain unchanged/);
  assert.match(structureRoute, /export async function PUT/);
});

test("parallel group management and effective timetable capacity are visible", () => {
  const groups = read("components/academics/class-subject-actions.tsx");
  const requirements = read("components/timetable/class-subject-requirements-editor.tsx");
  const resetRoute = read("app/api/academics/classes/[id]/parallel-subject-groups/reset/route.ts");

  assert.match(groups, /Configured groups/);
  assert.match(groups, /Reset Parallel Groups/);
  assert.match(groups, /Delete/);
  assert.match(resetRoute, /authenticatedBackendFetch/);
  assert.match(requirements, /Parallel savings/);
  assert.match(requirements, /Timetable periods required/);
});
