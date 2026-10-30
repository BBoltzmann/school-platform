import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ studentId: string; assessmentId: string; action: string }> };
export async function POST(request: Request, { params }: Context) {
  const { studentId, assessmentId, action } = await params;
  if (action !== "void" && action !== "restore")
    return NextResponse.json({ error: "Invalid correction action." }, { status: 400 });
  const body = await request.json();
  const response = await authenticatedBackendFetch(
    `/api/students/${encodeURIComponent(studentId)}/report-scores/${encodeURIComponent(assessmentId)}/${action}`,
    { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  if (response.status === 204) return new Response(null, { status: 204 });
  return NextResponse.json(await response.json().catch(() => ({})), { status: response.status });
}
