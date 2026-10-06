import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function POST(request: Request) {
  const response = await authenticatedBackendFetch("/api/academics/campuses", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: await request.text(),
  });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json(), { status: response.status });
}
