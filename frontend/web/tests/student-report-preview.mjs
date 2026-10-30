import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '../../../');
const read = (file) => readFileSync(path.join(root, file), 'utf8');

test('student report preview is guarded by student permission and tenant', () => {
  const api = read('backend/src/SchoolPlatform.Api/Endpoints/StudentReportPreviewEndpoints.cs');
  assert.match(api, /HasPermission\("students\.read"\)/);
  assert.match(api, /HasPermission\("teacher\.students\.read"\)/);
  assert.match(api, /!adminAccess && !teacherAccess/);
  assert.match(api, /!enrollment\.IsCurrent \|\| !enrollment\.IsActive/);
  assert.match(api, /x\.UserId == currentUser\.UserId/);
  assert.match(api, /x\.StaffMemberId == staffId\.Value/);
  assert.match(api, /x\.ClassGroupId == enrollment\.ClassGroupId && x\.IsActive/);
  assert.match(api, /x\.AcademicSessionId == enrollment\.AcademicSessionId/);
  assert.match(api, /x\.TenantId == tenantId && x\.Id == studentId/);
  assert.match(api, /RequireAuthorization\(\)/);
});

test('midterm draft is provisional and requires all first-half marks', () => {
  const api = read('backend/src/SchoolPlatform.Api/Endpoints/StudentReportPreviewEndpoints.cs');
  assert.match(api, /canGenerateOfficial = false/);
  assert.match(api, /reportKind = provisionalMidtermReady \? "ProvisionalMidtermDraft" : "TermToDateDraft"/);
  assert.match(api, /subjectsAtFirstHalfWeight == subjects\.Length/);
  assert.match(api, /subjects\.All\(x => x\.MarksComplete\)/);
  assert.match(api, /!hasUnresolvedElectives && unmatchedRecordedSubjects\.Length == 0/);
  assert.match(api, /hasUnresolvedElectives/);
  assert.match(api, /raw\.HasValue/);
  assert.match(api, /MissingScores/);
});

test('report is linked from the student profile and print is labelled draft', () => {
  const student = read('frontend/web/src/app/app/[tenantSlug]/(portal)/students/[id]/page.tsx');
  const preview = read('frontend/web/src/components/reports/student-report-preview.tsx');
  assert.match(student, /students\/\$\{student\.id\}\/reports/);
  assert.match(preview, /Print Draft/);
  assert.match(preview, /not an official report/);
  assert.match(preview, /Not recorded/);
});

test('teacher My Students links to the shared report preview with class access enforced on the API', () => {
  const view = read('frontend/web/src/components/teacher/teacher-portal-view.tsx');
  const section = read('frontend/web/src/app/app/[tenantSlug]/(portal)/teacher/[section]/page.tsx');
  const home = read('frontend/web/src/app/app/[tenantSlug]/(portal)/teacher/page.tsx');
  const teacher = read('frontend/web/src/app/app/[tenantSlug]/(portal)/teacher/students/[studentId]/reports/page.tsx');
  const admin = read('frontend/web/src/app/app/[tenantSlug]/(portal)/students/[id]/reports/page.tsx');
  assert.match(view, /teacher\/students\/\$\{item\.id\}\/reports/);
  assert.match(view, /Report Card/);
  assert.match(section, /tenantSlug=\{tenantSlug\}/);
  assert.match(home, /tenantSlug=\{tenantSlug\}/);
  assert.match(teacher, /StudentReportPreview/);
  assert.match(teacher, /Back to My Students/);
  assert.match(admin, /StudentReportPreview/);
  assert.match(admin, /Back to Student Profile/);
});

test('guardians do not receive report generation or draft read privileges', () => {
  const api = read('backend/src/SchoolPlatform.Api/Endpoints/StudentReportPreviewEndpoints.cs');
  assert.doesNotMatch(api, /HasPermission\("guardians\./);
  assert.match(api, /canGenerateOfficial = false/);
  assert.match(api, /RequireAuthorization\(\)/);
});
