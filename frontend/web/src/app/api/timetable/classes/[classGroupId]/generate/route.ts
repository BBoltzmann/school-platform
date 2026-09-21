import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ classGroupId: string }> };
export async function POST(request: Request, context: Context) {
  const { classGroupId } = await context.params;
  const response = await authenticatedBackendFetch(`/api/timetable/classes/${classGroupId}/generate`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(await request.json()) });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json().catch(() => ({})), { status: response.status });
}
