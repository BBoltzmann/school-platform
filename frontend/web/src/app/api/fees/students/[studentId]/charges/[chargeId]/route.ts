import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ studentId: string; chargeId: string }> };

export async function PATCH(request: Request, context: Context) {
  const { studentId, chargeId } = await context.params;
  const response = await authenticatedBackendFetch(`/api/fees/students/${studentId}/charges/${chargeId}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(await request.json()),
  });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  const text = await response.text();
  let result: unknown = null;
  try { result = text ? JSON.parse(text) : null; } catch { result = { error: "Fee service returned an invalid response." }; }
  return NextResponse.json(result, { status: response.status });
}
