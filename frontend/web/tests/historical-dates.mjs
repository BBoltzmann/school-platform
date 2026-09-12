import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

test('student historical joining dates are not constrained by session boundaries', () => {
  const file = fs.readFileSync('src/components/students/change-student-placement-dialog.tsx', 'utf8');
  assert.doesNotMatch(file, /min=|max=|Must fall inside the current/);
  assert.match(file, /date the student joined the school/);
});

test('staff employment date controls do not impose a frontend current-date minimum', () => {
  for (const fileName of ['src/components/staff/create-staff-dialog.tsx', 'src/components/staff/edit-staff-dialog.tsx']) {
    const file = fs.readFileSync(fileName, 'utf8');
    assert.doesNotMatch(file, /min=\s*\{?\s*(today|current|new Date|Date\.now)/i);
  }
});
