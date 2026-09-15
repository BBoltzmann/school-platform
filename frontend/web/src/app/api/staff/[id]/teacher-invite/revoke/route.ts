import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
export async function POST(_: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const response = await authenticatedBackendFetch(`/api/staff/${id}/teacher-invite/revoke`, { method: "POST", cache: "no-store" });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return new NextResponse(null, { status: response.status });
}
