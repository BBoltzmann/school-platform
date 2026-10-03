import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const root = new URL("../src/", import.meta.url);
const read = (file) => fs.readFileSync(new URL(file, root), "utf8");

test("optional fee components use dedicated authenticated endpoints and retain provenance", () => {
  const route = read("app/api/fees/students/[studentId]/optional-components/route.ts");
  const workspace = read("components/fees/student-accounts-workspace.tsx");
  assert.match(route, /authenticatedBackendFetch/);
  assert.match(route, /export async function GET/);
  assert.match(route, /export async function POST/);
  assert.match(workspace, /Add Optional Fee Component/);
  assert.match(workspace, /templateAmount/);
  assert.match(workspace, /alreadyAdded/);
  assert.match(workspace, /feeStructureLineId/);
});

test("optional fee components are available before recording a payment", () => {
  const workspace = read("components/fees/record-payment-workspace.tsx");
  assert.match(workspace, /Add Optional Component/);
  assert.match(workspace, /optional-components/);
  assert.match(workspace, /next payment allocation/);
});
