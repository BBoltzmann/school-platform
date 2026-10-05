import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
const root = new URL("../src/", import.meta.url);
const read = file => fs.readFileSync(new URL(file, root), "utf8");

test("student accounts retain the detailed account workflow and finance actions", () => {
  const page = read("components/fees/student-accounts-workspace.tsx");
  assert.match(page, /View Account/);
  assert.match(page, /Payment History/);
  assert.match(page, /Add discount/);
  assert.match(page, /Add previous balance/);
  assert.match(page, /discountsApplied/);
  assert.match(page, /Reverse discount/);
});

test("fees management exposes student, class, and fee-structure discount application", () => {
  const page = read("components/fees/discounts-workspace.tsx");
  assert.match(page, /Student/);
  assert.match(page, /Class/);
  assert.match(page, /FeeStructure/);
  assert.match(page, /preview/);
  assert.match(page, /Confirm and apply/);
});

test("carry-forward preview supports explicit student exclusions", () => {
  const page = read("components/fees/carry-forward-workspace.tsx");
  assert.match(page, /studentIds/);
  assert.match(page, /type="checkbox"/);
  assert.match(page, /Carry selected balances/);
});

test("the Finance navigation entry remains separate from Fees Management", () => {
  const navigation = read("components/navigation/admin-navigation.ts");
  assert.match(navigation, /title: "Finance"/);
  assert.match(navigation, /href: "finance"/);
  assert.match(navigation, /title: "Fees Management"/);
});
