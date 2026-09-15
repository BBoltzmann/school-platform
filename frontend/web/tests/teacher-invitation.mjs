import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
const root = new URL("../src/", import.meta.url);
test("teacher invitation activation and admin controls are wired", () => {
  const activation = fs.readFileSync(new URL("app/teacher/activate/page.tsx", root), "utf8");
  const staff = fs.readFileSync(new URL("components/staff/teacher-invite-actions.tsx", root), "utf8");
  assert.match(activation, /Activate Your Teacher Account/);
  assert.match(activation, /teacher-invite\/activate/);
  assert.match(staff, /teacher-invite/);
  assert.match(staff, /Copy Invite/);
});
