import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import vm from "node:vm";
import ts from "typescript";
import { Buffer } from "node:buffer";

const root = new URL("../src/", import.meta.url);

function loadBuilder() {
  const file = fs.readFileSync(new URL("lib/reports/subject-gradebook-xlsx.ts", root), "utf8");
  const compiled = ts.transpileModule(file, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const exports = {};
  vm.runInNewContext(compiled, {
    exports,
    require: id => id === "node:buffer" ? { Buffer } : assert.fail(`Unexpected import: ${id}`),
  });
  return exports.buildSubjectGradebookXlsx;
}

function zipEntries(buffer) {
  const entries = new Map();
  let pos = 0;
  while (buffer.readUInt32LE(pos) === 0x04034b50) {
    const compressedSize = buffer.readUInt32LE(pos + 18);
    const nameLength = buffer.readUInt16LE(pos + 26);
    const extraLength = buffer.readUInt16LE(pos + 28);
    const name = buffer.toString("utf8", pos + 30, pos + 30 + nameLength);
    const start = pos + 30 + nameLength + extraLength;
    entries.set(name, buffer.toString("utf8", start, start + compressedSize));
    pos = start + compressedSize;
  }
  return entries;
}

test("Excel export produces an XLSX ZIP containing numeric scores and explicit incompleteness", () => {
  const build = loadBuilder();
  const workbook = build({
    academicTermId: "00000000-0000-0000-0000-000000000001",
    classGroupName: "JS 1 A", subjectName: "Mathematics",
    configuredWeightTotal: 100,
    assessments: [
      { assessmentId: "a", title: "Assignment", maximumScore: 20, weightPercentage: 40 },
      { assessmentId: "b", title: "Exam", maximumScore: 100, weightPercentage: 60 },
    ],
    students: [
      { admissionNumber: "0001", studentName: "Alice", totalWeightedScore: 82, scores: [{ assessmentId: "a", rawScore: 20, weightedContribution: 40 }, { assessmentId: "b", rawScore: 70, weightedContribution: 42 }] },
      { admissionNumber: "0002", studentName: "Bob", totalWeightedScore: 20, scores: [{ assessmentId: "a", rawScore: 10, weightedContribution: 20 }, { assessmentId: "b", rawScore: null, weightedContribution: null }] },
    ],
  });
  assert.equal(workbook.readUInt32LE(0), 0x04034b50);
  const entries = zipEntries(workbook);
  assert.ok(entries.has("xl/workbook.xml"));
  const sheet = entries.get("xl/worksheets/sheet1.xml");
  assert.match(sheet, /All scores entered/);
  assert.match(sheet, /Incomplete — do not publish/);
  assert.match(sheet, /<v>82<\/v>/);
  assert.match(sheet, /Scores entered/);
  assert.match(sheet, /<c r="E8"\/><c r="F8"\/>/, "Missing exam scores stay blank, not zero");
});

test("names beginning with formulas are written as plain text, never formulas", () => {
  const build = loadBuilder();
  const workbook = build({
    academicTermId: "id", classGroupName: "A", subjectName: "B", configuredWeightTotal: 90,
    assessments: [{ assessmentId: "a", title: "=<bad>", maximumScore: 10, weightPercentage: 90 }],
    students: [{ admissionNumber: "0", studentName: '=HYPERLINK("https://example.invalid")', totalWeightedScore: 0, scores: [{ assessmentId: "a", rawScore: null, weightedContribution: null }] }],
  });
  const sheet = zipEntries(workbook).get("xl/worksheets/sheet1.xml");
  assert.match(sheet, /&lt;bad&gt;/);
  assert.match(sheet, /t="inlineStr"/);
  assert.doesNotMatch(sheet, /<f>/);
  assert.match(sheet, /INCOMPLETE WEIGHTS/);
});

test("export route uses existing protected gradebook API, no guardian disclosure", () => {
  const route = fs.readFileSync(new URL("app/api/assessments/gradebook/export/route.ts", root), "utf8");
  assert.match(route, /authenticatedBackendFetch/);
  assert.match(route, /api\/assessments\/gradebook/);
  assert.match(route, /private, no-store/);
  assert.match(route, /status: backendResponse.status/);
  assert.doesNotMatch(route, /guardian|publicApi/i);
});

async function callExport({ backend, query = "academicTermId=11111111-1111-1111-1111-111111111111&classGroupId=22222222-2222-2222-2222-222222222222&subjectId=33333333-3333-3333-3333-333333333333" }) {
  const file = fs.readFileSync(new URL("app/api/assessments/gradebook/export/route.ts", root), "utf8");
  const compiled = ts.transpileModule(file, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const exports = {};
  vm.runInNewContext(compiled, {
    exports, URL, URLSearchParams, Response, Uint8Array,
    require: id => id === "@/lib/api/authenticated-backend"
      ? { authenticatedBackendFetch: backend }
      : id === "@/lib/reports/subject-gradebook-xlsx"
        ? { buildSubjectGradebookXlsx: loadBuilder() }
        : assert.fail(`Unexpected route import: ${id}`),
  });
  return exports.GET(new Request(`https://example.test/api/assessments/gradebook/export?${query}`));
}

test("export endpoint validates IDs before calling backend and retains authorization status", async () => {
  const invalid = await callExport({ backend: () => assert.fail("Invalid IDs must not reach backend"), query: "academicTermId=oops" });
  assert.equal(invalid.status, 400);
  const denied = await callExport({ backend: () => new Response("Denied", { status: 403 }) });
  assert.equal(denied.status, 403);
  const unauthenticated = await callExport({ backend: () => null });
  assert.equal(unauthenticated.status, 401);
});

test("successful response is private XLSX attachment", async () => {
  const response = await callExport({ backend: () => Response.json({
    academicTermId: "00000000-0000-0000-0000-000000000001", classGroupName: "A", subjectName: "B", configuredWeightTotal: 100,
    assessments: [{ assessmentId: "x", title: "Exam", maximumScore: 100, weightPercentage: 100 }], students: [],
  }) });
  assert.equal(response.status, 200);
  assert.match(response.headers.get("content-type"), /spreadsheetml/);
  assert.match(response.headers.get("content-disposition"), /attachment/);
  assert.match(response.headers.get("cache-control"), /no-store/);
  assert.equal(Buffer.from(await response.arrayBuffer()).readUInt32LE(0), 0x04034b50);
});
