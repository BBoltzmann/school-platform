import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ academicTermId: string }> };
export async function GET(request: Request, context: Context) {
  const { academicTermId } = await context.params;
  const response = await authenticatedBackendFetch(`/api/timetable/generated/${academicTermId}/history`);
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json().catch(() => []), { status: response.status });
}
