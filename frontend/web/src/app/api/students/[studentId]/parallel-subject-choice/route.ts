import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ studentId: string }> };
export async function PUT(request: Request, { params }: Context) {
  const { studentId } = await params;
  const body = await request.json();
  const response = await authenticatedBackendFetch(
    `/api/students/${encodeURIComponent(studentId)}/parallel-subject-choice`,
    { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  if (response.status === 204) return new Response(null, { status: 204 });
  return NextResponse.json(await response.json().catch(() => ({})), { status: response.status });
}
