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
