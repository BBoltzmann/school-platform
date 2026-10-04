import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
export async function GET() {
  const response = await authenticatedBackendFetch("/api/platform-admin/dashboard");
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json(), { status: response.status });
}
