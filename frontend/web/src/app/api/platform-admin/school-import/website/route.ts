import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function POST(request: Request) {
  const response = await authenticatedBackendFetch("/api/platform-admin/school-import/website", { method: "POST", headers: { "Content-Type": "application/json" }, body: await request.text() });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return new NextResponse(await response.text(), { status: response.status, headers: { "Content-Type": response.headers.get("content-type") ?? "application/json" } });
}
