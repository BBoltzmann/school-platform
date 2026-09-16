import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function POST(_: Request, { params }: { params: Promise<{ academicTermId: string }> }) {
  const { academicTermId } = await params;
  const response = await authenticatedBackendFetch(`/api/timetable/generated/${academicTermId}/reset`, { method: "POST" });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  if (response.status === 204) return new NextResponse(null, { status: 204 });
  let result: unknown = {};
  try { result = await response.json(); } catch { /* empty response */ }
  return NextResponse.json(result, { status: response.status });
}
