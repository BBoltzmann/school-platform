import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function POST(request: Request, context: { params: Promise<{ applicationId: string }> }) {
  const { applicationId } = await context.params;
  const response = await authenticatedBackendFetch(`/api/fees/discounts/applications/${applicationId}/reverse`, { method: "POST", headers: { "Content-Type": "application/json" }, body: await request.text() });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json(), { status: response.status });
}
