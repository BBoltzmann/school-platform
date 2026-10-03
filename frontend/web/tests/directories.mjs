import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const root = new URL("../src/", import.meta.url);
const read = (file) => fs.readFileSync(new URL(file, root), "utf8");

test("student directory supports academic ordering, class filtering and combined search", () => {
  const source = read("components/students/students-directory.tsx");
  assert.match(source, /Class then Name/);
  assert.match(source, /classGroupId/);
  assert.match(source, /admissionNumber/);
  assert.match(source, /academicLevelName/);
  assert.match(source, /classOrder/);
  assert.match(source, /No matching students/);
});

test("staff directory supports compact filtered views and details navigation", () => {
  const source = read("components/staff/staff-directory.tsx");
  assert.match(source, /Teaching/);
  assert.match(source, /Non-Teaching/);
  assert.match(source, /employmentType/);
  assert.match(source, /Search name, number, role or contact/);
  assert.match(source, /View/);
  assert.match(source, /status/);
});

test("shared table rows provide subtle alternating backgrounds without overriding state", () => {
  const source = read("components/ui/table.tsx");
  assert.match(source, /odd:bg-background/);
  assert.match(source, /even:bg-muted\/20/);
  assert.match(source, /data-\[state=selected\]:bg-muted/);
});
