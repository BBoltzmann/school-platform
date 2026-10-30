import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const endpoint = readFileSync("../../backend/src/SchoolPlatform.Api/Endpoints/StudentReportPreviewEndpoints.cs", "utf8");
const view = readFileSync("src/components/reports/student-report-preview.tsx", "utf8");

test("first-half default uses 20/20/60 without changing scores", () => {
  assert.match(endpoint, /const decimal firstHalfWeight = 20m/);
  assert.match(endpoint, /const decimal secondHalfWeight = 20m/);
  assert.match(endpoint, /const decimal finalExamWeight = 60m/);
  assert.match(endpoint, /x\.ConfiguredWeight == firstHalfWeight/);
  assert.match(view, /The final exam may be entered out of 100 and weighted to 60%/);
});

test("mismatch warning only reports this student's saved unmatched marks", () => {
  assert.match(endpoint, /!offeredIds\.Contains\(a\.SubjectId\) && scores\.ContainsKey\(a\.Id\)/);
  assert.match(endpoint, /unmatchedRecordedSubjects\.Length > 0/);
  assert.doesNotMatch(endpoint, /Assessments exist outside the currently offered class subjects/);
  assert.match(view, /Saved marks requiring subject-offering review/);
});

test("draft readiness requires first-half allocation, saved marks, and resolved subjects", () => {
  assert.match(endpoint, /subjectsAtFirstHalfWeight == subjects\.Length/);
  assert.match(endpoint, /subjects\.All\(x => x\.MarksComplete\)/);
  assert.match(endpoint, /!hasUnresolvedElectives && unmatchedRecordedSubjects\.Length == 0/);
  assert.match(endpoint, /canGenerateOfficial = false/);
  assert.match(view, /Print Midterm Draft/);
});

test("teacher access still scoped to tenant, class, session and active assignment", () => {
  assert.match(endpoint, /x\.TenantId == tenantId && x\.StudentId == studentId/);
  assert.match(endpoint, /x\.AcademicSessionId == enrollment\.AcademicSessionId &&/);
  assert.match(endpoint, /x\.ClassGroupId == enrollment\.ClassGroupId && x\.IsActive/);
  assert.match(endpoint, /return Results\.Forbid\(\)/);
});
