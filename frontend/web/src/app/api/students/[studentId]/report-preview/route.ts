import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ studentId: string }> };

export async function GET(request: Request, context: Context) {
  const { studentId } = await context.params;
  const { searchParams } = new URL(request.url);
  const term = searchParams.get("academicTermId");
  const query = term ? `?academicTermId=${encodeURIComponent(term)}` : "";
  const response = await authenticatedBackendFetch(
    `/api/students/${encodeURIComponent(studentId)}/report-preview${query}`
  );
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  const body = await response.json().catch(() => ({ error: "Unable to read report response." }));
  return NextResponse.json(body, { status: response.status, headers: { "Cache-Control": "no-store" } });
}
