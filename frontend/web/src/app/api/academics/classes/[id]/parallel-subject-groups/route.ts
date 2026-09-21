import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ id: string }> };

async function proxy(request: Request, context: Context, method: string) {
  const { id } = await context.params;
  const url = new URL(request.url);
  const response = await authenticatedBackendFetch(`/api/academics/classes/${id}/parallel-subject-groups${url.search}`, {
    method,
    headers: { "Content-Type": "application/json" },
    ...(method === "POST" ? { body: JSON.stringify(await request.json()) } : {}),
  });
  if (!response) return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  return NextResponse.json(await response.json().catch(() => ({})), { status: response.status });
}

export async function GET(request: Request, context: Context) { return proxy(request, context, "GET"); }
export async function POST(request: Request, context: Context) { return proxy(request, context, "POST"); }
