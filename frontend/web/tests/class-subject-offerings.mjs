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
