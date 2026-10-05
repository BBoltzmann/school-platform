import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

async function forward(request: Request, path: string) {
  const response = await authenticatedBackendFetch(path, { method: "POST", headers: { "Content-Type": "application/json" }, body: await request.text() });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json(), { status: response.status });
}
export async function POST(request: Request) { return forward(request, "/api/fees/carry-forward"); }
