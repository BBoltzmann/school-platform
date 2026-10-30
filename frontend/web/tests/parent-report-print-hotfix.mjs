import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const read = (p) => readFileSync(`../../${p}`, "utf8");
const preview = read("frontend/web/src/components/reports/student-report-preview.tsx");
const parent = read("frontend/web/src/components/reports/parent-midterm-report.tsx");
const styles = read("frontend/web/src/components/reports/parent-midterm-report.module.css");
const route = read("frontend/web/src/app/app/[tenantSlug]/reports/student/[studentId]/parent-summary/page.tsx");

test("staff keeps its detailed print button and gains a separate printable parent report", () => {
  assert.match(preview, /window\.print\(\)/);
  assert.match(preview, /Print Parent Report \(A4\)/);
  assert.match(preview, /\/parent-summary\?academicTermId=/);
  assert.match(preview, /target="_blank"/);
});

test("print page is separate from admin shell, authorised and reuses live report endpoint", () => {
  assert.match(route, /getSessionContext\(\)/);
  assert.match(route, /session\.tenantSlug !== tenantSlug/);
  assert.match(parent, /\/api\/students\/\$\{encodeURIComponent\(studentId\)\}\/report-preview/);
  assert.match(parent, /cache: "no-store"/);
  assert.match(parent, /schoolName=|schoolName, logoDataUrl/);
});

test("parent report uses readable test comments, not weighted-contribution jargon", () => {
  for (const term of ["Excellent", "Very good", "Satisfactory", "Needs improvement", "Poor"]) {
    assert.ok(parent.includes(term));
  }
  assert.match(parent, /mark\.rawScore \/ mark\.maximumScore/);
  assert.doesNotMatch(parent, /weightPercentage|weightedContribution|configuredWeight/);
  assert.match(parent, /Projected scores are not final-term examination results/);
});

test("colored SVG bars and numeric comparisons survive print without relying on background colors", () => {
  assert.match(parent, /<svg/);
  assert.match(parent, /<rect/);
  assert.match(parent, /fill=\{gradeColor/);
  assert.match(parent, /classAverageCount/);
  assert.match(styles, /print-color-adjust: exact/);
  assert.match(styles, /@page \{ size: A4 portrait; margin: 0; \}/);
});

test("print layout has at most two deliberate sheets and compact subject rows", () => {
  assert.match(parent, /Math\.ceil\(subjects\.length \/ 2\)/);
  assert.match(parent, /<SubjectTable subjects=\{pageSubjects\}/);
  assert.match(parent, /pages\.map\(/);
  assert.match(styles, /width: 210mm/);
  assert.match(styles, /height: 297mm/);
  assert.match(styles, /break-after: page/);
  assert.match(parent, /Parent \/ guardian follow-up/);
});
