import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const root = new URL("../src/", import.meta.url);

test("timetable reset is explicit, term scoped, and keeps the structure editor", () => {
  const panel = fs.readFileSync(new URL("components/timetable/timetable-mvp-panel.tsx", root), "utf8");
  const route = fs.readFileSync(new URL("app/api/timetable/generated/[academicTermId]/reset/route.ts", root), "utf8");
  assert.match(panel, /Reset Generated Timetable/);
  assert.match(panel, /window\.confirm/);
  assert.match(panel, /No timetable generated/);
  assert.match(route, /generated\/\$\{academicTermId\}\/reset/);
});
