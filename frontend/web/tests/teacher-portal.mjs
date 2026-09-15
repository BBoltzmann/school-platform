import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const root = new URL("../src/", import.meta.url);
const read = (file) => fs.readFileSync(new URL(file, root), "utf8");

test("teacher portal has role-specific navigation and scoped portal views", () => {
  assert.match(read("components/navigation/teacher-navigation.ts"), /My Classes/);
  assert.match(read("components/navigation/teacher-navigation.ts"), /My Students/);
  assert.match(read("components/navigation/teacher-navigation.ts"), /My Timetable/);
  assert.match(read("components/navigation/admin-sidebar.tsx"), /roles.includes\("Teacher"\)/);
  assert.match(read("components/teacher/teacher-portal-view.tsx"), /No teaching assignments yet/);
  assert.match(read("app/app/[tenantSlug]/dashboard/page.tsx"), /teacher-portal/);
});
