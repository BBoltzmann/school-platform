import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const root = new URL("../src/", import.meta.url);
const read = (file) => fs.readFileSync(new URL(file, root), "utf8");

test("timetable reset is explicit, term scoped, and keeps the structure editor", () => {
  const panel = fs.readFileSync(new URL("components/timetable/timetable-mvp-panel.tsx", root), "utf8");
  const route = fs.readFileSync(new URL("app/api/timetable/generated/[academicTermId]/reset/route.ts", root), "utf8");
  assert.match(panel, /Reset Generated Timetable/);
  assert.match(panel, /window\.confirm/);
  assert.match(panel, /No timetable generated/);
  assert.match(route, /generated\/\$\{academicTermId\}\/reset/);
});

test("active timetable has master, class, teacher, and teacher portal exports", () => {
  const panel = fs.readFileSync(new URL("components/timetable/timetable-mvp-panel.tsx", root), "utf8");
  const teacher = fs.readFileSync(new URL("components/teacher/teacher-portal-view.tsx", root), "utf8");
  const pdf = fs.readFileSync(new URL("lib/timetable-pdf.ts", root), "utf8");
  assert.match(panel, /Master School Timetable/);
  assert.match(panel, /Download Master PDF/);
  assert.match(panel, /Download Class PDF/);
  assert.match(panel, /Download Teacher PDF/);
  assert.match(panel, /periodColumns/);
  assert.match(panel, /schoolName/);
  assert.match(teacher, /Download PDF/);
  assert.match(pdf, /application\/pdf/);
  assert.doesNotMatch(pdf, /Antioch Royal College/);
  assert.match(read("types/session.ts"), /tenantName/);
  assert.match(read("app/app/[tenantSlug]/layout.tsx"), /session\.tenantName/);
});

test("parallel member rows use stable subject and teacher identifiers for React keys", () => {
  const panel = fs.readFileSync(new URL("components/timetable/timetable-mvp-panel.tsx", root), "utf8");
  assert.match(panel, /\$\{entry\.id\}-\$\{member\.subjectId\}-\$\{member\.staffMemberId\}/);
  assert.match(panel, /subjectId: entry\.subjectId, staffMemberId: entry\.staffMemberId/);
  assert.match(panel, /membersByOccurrence/);
  assert.match(panel, /if \(!memberKeys\.has\(memberKey\)\)/);
});
