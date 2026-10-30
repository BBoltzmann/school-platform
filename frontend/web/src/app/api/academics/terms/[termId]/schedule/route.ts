import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ termId: string }> };

async function forward(termId: string, method: "GET" | "PUT", body?: string) {
  const response = await authenticatedBackendFetch(
    `/api/academics/terms/${encodeURIComponent(termId)}/schedule`,
    {
      method,
      ...(body ? { headers: { "Content-Type": "application/json" }, body } : {}),
    }
  );
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  if (response.status === 204) return new NextResponse(null, { status: 204 });
  const payload = await response.text();
  return new NextResponse(payload, {
    status: response.status,
    headers: { "Content-Type": response.headers.get("content-type") ?? "application/json" },
  });
}

export async function GET(_request: Request, context: Context) {
  const { termId } = await context.params;
  return forward(termId, "GET");
}

export async function PUT(request: Request, context: Context) {
  const { termId } = await context.params;
  return forward(termId, "PUT", await request.text());
}
