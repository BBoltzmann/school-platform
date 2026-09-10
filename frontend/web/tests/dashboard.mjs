import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

const page = fs.readFileSync('src/app/app/[tenantSlug]/dashboard/page.tsx', 'utf8');
const header = fs.readFileSync('src/components/layout/admin-header.tsx', 'utf8');
const api = fs.readFileSync('src/lib/api/dashboard.ts', 'utf8');

test('dashboard is API-driven and has honest new-school states', () => {
  assert.match(api, /\/api\/dashboard/);
  for (const placeholder of ['1,248', '18.54M', '2026/2027', 'John Okafor', 'School Resumption', '3.2%', '8.7%']) {
    assert.doesNotMatch(page, new RegExp(placeholder.replace('.', '\\.'), 'i'));
  }
  for (const text of ['Set up your school', 'Create Academic Session', 'No students added yet', 'No staff added yet', 'No pending applications', 'No fee payments recorded', 'No recent activity yet', 'No calendar events yet']) {
    assert.match(page, new RegExp(text));
  }
});

test('dashboard has no fabricated notification or message counts', () => {
  assert.doesNotMatch(header, />3<|>7</);
  assert.doesNotMatch(header, /Notifications|Messages/);
});
