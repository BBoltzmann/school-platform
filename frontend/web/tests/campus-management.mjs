import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
const root = new URL("../src/", import.meta.url);
const read = file => fs.readFileSync(new URL(file, root), "utf8");

test("academic setup exposes tenant campus management", () => {
  const page = read("app/app/[tenantSlug]/(portal)/academics/page.tsx");
  const dialog = read("components/academics/create-campus-dialog.tsx");
  assert.match(page, /title="Campuses"/);
  assert.match(page, /CreateCampusDialog/);
  assert.match(dialog, /api\/academics\/campuses/);
  assert.match(dialog, /Campus name/);
  assert.match(dialog, /Create campus/);
});
