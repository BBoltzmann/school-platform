import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ studentId: string }> };

export async function GET(request: Request, context: Context) {
  const { studentId } = await context.params;
  const termId = new URL(request.url).searchParams.get("academicTermId");
  const response = await authenticatedBackendFetch(
    `/api/fees/students/${studentId}/optional-components?academicTermId=${encodeURIComponent(termId ?? "")}`
  );
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  const body = await response.text();
  let result: unknown = null;
  try { result = body ? JSON.parse(body) : null; } catch { result = { error: "Fee service returned an invalid response." }; }
  return NextResponse.json(result, { status: response.status });
}

export async function POST(request: Request, context: Context) {
  const { studentId } = await context.params;
  const body = await request.json();
  const response = await authenticatedBackendFetch(`/api/fees/students/${studentId}/optional-components`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  const text = await response.text();
  let result: unknown = null;
  try { result = text ? JSON.parse(text) : null; } catch { result = { error: "Fee service returned an invalid response." }; }
  return NextResponse.json(result, { status: response.status });
}
