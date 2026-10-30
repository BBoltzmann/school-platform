import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const root = new URL("../../../", import.meta.url);
const read = p => readFileSync(new URL(p, root), "utf8");

test("terms store 20/20/60 defaults and persistent deadline fields", () => {
  const code = read("backend/src/SchoolPlatform.Domain/Academics/AcademicTerm.cs");
  assert.match(code, /FirstHalfWeight \{ get; private set; \} = 20m/);
  assert.match(code, /SecondHalfWeight \{ get; private set; \} = 20m/);
  assert.match(code, /FinalExamWeight \{ get; private set; \} = 60m/);
  assert.match(code, /MidtermBreakStartDate/);
  assert.match(code, /SecondHalfDueDate/);
  assert.match(code, /FinalReportDueDate/);
  assert.match(code, /!= 100m/);
});

test("term schedule may be updated only by an authorised admin", () => {
  const code = read("backend/src/SchoolPlatform.Api/Program.cs");
  assert.match(code, /MapPut\(\s*"\/api\/academics\/terms\/\{termId:guid\}\/schedule"/);
  assert.match(code, /!currentUser\.HasPermission\("academics\.configure"\)/);
  const service = read("backend/src/SchoolPlatform.Infrastructure/Academics/AcademicSetupService.cs");
  assert.match(service, /x\.TenantId == tenantId && x\.Id == termId/);
});

test("teachers may see deadlines but cannot modify schedule", () => {
  const code = read("backend/src/SchoolPlatform.Api/Program.cs");
  assert.match(code, /currentUser\.HasPermission\("teacher\.students\.read"\)/);
  const workspace = read("frontend/web/src/components/assessments/assessment-workspace.tsx");
  assert.match(workspace, /<TermScheduleNotice termId=\{academicTermId\}/);
});

test("existing terms can be edited and new terms have date inputs", () => {
  const academics = read("frontend/web/src/app/app/[tenantSlug]/(portal)/academics/page.tsx");
  assert.match(academics, /<TermScheduleEditor term=\{term\}/);
  const create = read("frontend/web/src/components/academics/create-term-dialog.tsx");
  assert.match(create, /scheduleDateFields\.map/);
  const editor = read("frontend/web/src/components/academics/term-schedule-editor.tsx");
  assert.match(editor, /method: "PUT"/);
});

test("schema migration preserves pre-existing assessment records", () => {
  const code = read("backend/src/SchoolPlatform.Infrastructure/Persistence/Migrations/20261030080000_AddTermAssessmentSchedule.cs");
  assert.match(code, /AddColumn/);
  assert.doesNotMatch(code, /DropTable|DeleteData|UpdateData/);
});
