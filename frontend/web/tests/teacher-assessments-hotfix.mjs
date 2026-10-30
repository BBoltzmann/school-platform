import { readFileSync } from "node:fs";
import { test } from "node:test";
import assert from "node:assert/strict";

const source = (path) => readFileSync(new URL(`../src/${path}`, import.meta.url), "utf8");
const backend = (path) => readFileSync(new URL(`../../../backend/src/${path}`, import.meta.url), "utf8");

test("teacher navigation links to shared assessment workspace", () => {
  assert.match(source("components/navigation/teacher-navigation.ts"), /teacher\/assessments/);
  assert.match(source("app/app/[tenantSlug]/(portal)/teacher/[section]/page.tsx"), /AssessmentWorkspace/);
});

test("teacher selectors constrain class/subject combinations", () => {
  const workspace = source("components/assessments/assessment-workspace.tsx");
  assert.match(workspace, /allowedPairs\.some/);
  assert.match(workspace, /visibleSubjects\.map/);
  assert.match(workspace, /allowDelete &&/);
});

test("teacher assessment setup is scoped by tenant, user and current session", () => {
  const setup = backend("SchoolPlatform.Api/Endpoints/AssessmentSetupEndpoints.cs");
  assert.match(setup, /teacher\.subjects\.read/);
  assert.match(setup, /staff\.UserId == currentUser\.UserId/);
  assert.match(setup, /assignment\.AcademicSessionId == currentSession\.Id/);
  assert.match(setup, /assignments = visiblePairs/);
  assert.match(setup, /UsesCustomSubjectOffering/);
});

test("backend checks teacher assignments before allowing assessment creation", () => {
  const endpoint = backend("SchoolPlatform.Api/Endpoints/AssessmentEndpoints.cs");
  const service = backend("SchoolPlatform.Infrastructure/Assessments/AssessmentService.cs");
  assert.match(endpoint, /teacher\.subjects\.read/);
  assert.match(service, /await EnsureAssessmentAccessAsync\(\s*request\.AcademicTermId,/);
});
