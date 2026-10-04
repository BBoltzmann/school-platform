import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
const root = new URL("../src/", import.meta.url);
const read = file => fs.readFileSync(new URL(file, root), "utf8");

test("platform admin has a separate login and school control plane", () => {
  assert.match(read("app/api/auth/platform-login/route.ts"), /platform-login/);
  assert.match(read("app/api/platform-admin/schools/route.ts"), /authenticatedBackendFetch/);
  assert.match(read("app/super-admin/page.tsx"), /PlatformDashboard/);
  assert.match(read("components/platform/platform-schools.tsx"), /Search schools/);
  assert.match(read("components/platform/platform-schools.tsx"), /Administrators/);
  const login = read("components/platform/platform-login.tsx");
  assert.match(login, /<form onSubmit=\{submit\}/);
  assert.match(login, /<Button type="submit"/);
  assert.match(login, /setError\(result\.error/);
  assert.match(login, /router\.push\("\/super-admin"\)/);
  assert.match(read("app/super-admin/schools/new/page.tsx"), /Create School/);
  assert.match(read("app/super-admin/schools/[tenantId]/page.tsx"), /Suspend School/);
});
