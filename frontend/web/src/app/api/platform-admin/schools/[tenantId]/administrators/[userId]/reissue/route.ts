import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function POST(_: Request, { params }: { params: Promise<{ tenantId: string; userId: string }> }) {
  const { tenantId, userId } = await params;
  const response = await authenticatedBackendFetch(`/api/platform-admin/schools/${tenantId}/administrators/${userId}/reissue`, { method: "POST" });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return new NextResponse(await response.text(), { status: response.status, headers: { "Content-Type": response.headers.get("content-type") ?? "application/json" } });
}
