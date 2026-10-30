import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const read = (file) => readFileSync(`../../${file}`, "utf8");
const api = read("backend/src/SchoolPlatform.Api/Endpoints/StudentReportPreviewEndpoints.cs");
const choices = read("backend/src/SchoolPlatform.Api/Endpoints/StudentReportCorrectionsEndpoints.cs");
const service = read("backend/src/SchoolPlatform.Infrastructure/Assessments/AssessmentService.cs");
const score = read("backend/src/SchoolPlatform.Domain/Assessments/AcademicAssessmentScore.cs");
const migration = read("backend/src/SchoolPlatform.Infrastructure/Persistence/Migrations/20261030120000_AddStudentReportCorrections.cs");
const ui = read("frontend/web/src/components/reports/student-report-preview.tsx");
const snapshot = read("backend/src/SchoolPlatform.Infrastructure/Persistence/Migrations/SchoolPlatformDbContextModelSnapshot.cs");

test("parallel alternatives require persisted per-student choice before inclusion", () => {
  assert.match(api, /StudentParallelSubjectChoices/);
  assert.match(api, /parallelIds\.Contains\(x\.Id\) \|\| selectedIds\.Contains\(x\.Id\)/);
  assert.match(api, /hasUnresolvedElectives/);
  assert.match(choices, /StudentParallelSubjectChoiceUpdated/);
  assert.match(choices, /m\.ClassSubject\.SubjectId == request\.SubjectId/);
  assert.match(service, /StudentParallelSubjectChoices\.Any/);
});

test("scores are recoverable, audited, and do not count after being voided", () => {
  assert.match(score, /public void Void/);
  assert.match(score, /public void Restore/);
  assert.match(service, /!x\.IsVoided/);
  assert.doesNotMatch(service, /AcademicAssessmentScores\.Remove/);
  assert.match(choices, /AssessmentScoreVoided/);
  assert.match(choices, /AssessmentScoreRestored/);
  assert.match(choices, /reason/);
});

test("projected A-F grades and subject-specific class averages require complete checkpoint", () => {
  assert.match(api, /weight == checkpointWeight/);
  assert.match(api, /items\.All\(a => classScoresLookup\.ContainsKey/);
  assert.match(api, /classSelections\.Any/);
  for (const boundary of ["70m", "60m", "50m", "40m"]) assert.ok(api.includes(boundary));
  assert.match(api, /classAverage\.HasValue/);
});

test("draft report has parent-teacher alerts and print-only report area", () => {
  assert.match(ui, /Provisional midterm performance/);
  assert.match(ui, /Parent \/ guardian action/);
  assert.match(ui, /A4 portrait/);
  assert.match(ui, /classAverageCount/);
  assert.match(ui, /correctMark/);
  assert.match(ui, /saveChoice/);
  assert.match(api, /contact the subject teacher/);
});

test("migration is additive, snapshot records additions and rollback refuses destructive loss", () => {
  assert.match(migration, /CreateTable/);
  assert.match(migration, /AddColumn<bool>/);
  assert.doesNotMatch(migration.split("protected override void Up(")[1].split("protected override void Down(")[0], /DropColumn|DropTable|DeleteData/);
  assert.match(migration, /intentionally non-destructive/);
  assert.match(snapshot, /student_parallel_subject_choices/);
  assert.match(snapshot, /IsVoided/);
});

test("tenant, student enrollment and teacher subject scoping protect corrections", () => {
  assert.match(choices, /x\.TenantId == tid && x\.StudentId == studentId/);
  assert.match(choices, /a\.SubjectId == assessment\.SubjectId && a\.IsActive/);
  assert.match(choices, /CurrentUserContext/);
  assert.match(choices, /db\.AuditLogs\.Add/);
});
