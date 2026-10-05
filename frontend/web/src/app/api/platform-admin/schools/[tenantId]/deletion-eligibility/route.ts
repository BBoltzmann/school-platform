import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
export async function GET(_: Request, { params }: { params: Promise<{ tenantId: string }> }) {
  const { tenantId } = await params;
  const response = await authenticatedBackendFetch(`/api/platform-admin/schools/${tenantId}/deletion-eligibility`);
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return new NextResponse(await response.text(), { status: response.status, headers: { "Content-Type": response.headers.get("content-type") ?? "application/json" } });
}
