import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ id: string }> };

async function proxy(request: Request, context: Context, method: "GET" | "PUT") {
  const { id } = await context.params;
  const response = await authenticatedBackendFetch(`/api/academics/classes/${id}/subjects`, {
    method,
    headers: { "Content-Type": "application/json" },
    ...(method === "PUT" ? { body: JSON.stringify(await request.json()) } : {}),
  });

  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json().catch(() => ({})), { status: response.status });
}

export async function GET(request: Request, context: Context) {
  return proxy(request, context, "GET");
}

export async function PUT(request: Request, context: Context) {
  return proxy(request, context, "PUT");
}

export async function POST(request: Request, context: Context) {
  const { id } = await context.params;
  const query = new URL(request.url).search;
  const response = await authenticatedBackendFetch(`/api/academics/classes/${id}/subjects/reset${query}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
  });

  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json().catch(() => ({})), { status: response.status });
}
