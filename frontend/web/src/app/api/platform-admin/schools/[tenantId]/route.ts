import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
type Context = { params: Promise<{ tenantId: string }> };
export async function GET(_: Request, context: Context) {
  const { tenantId } = await context.params;
  const response = await authenticatedBackendFetch(`/api/platform-admin/schools/${tenantId}`);
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json(), { status: response.status });
}
export async function POST(request: Request, context: Context) {
  const { tenantId } = await context.params;
  const response = await authenticatedBackendFetch(`/api/platform-admin/schools/${tenantId}/status`, { method: "POST", headers: { "Content-Type": "application/json" }, body: await request.text() });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json(), { status: response.status });
}
export async function DELETE(_: Request, context: Context) {
  const { tenantId } = await context.params;
  const response = await authenticatedBackendFetch(`/api/platform-admin/schools/${tenantId}`, { method: "DELETE" });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  const body = await response.text();
  return new NextResponse(body, { status: response.status, headers: { "Content-Type": response.headers.get("content-type") ?? "application/json" } });
}
