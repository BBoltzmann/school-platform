import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = {
  params: Promise<{ studentId: string; paymentId: string }>;
};

export async function POST(request: Request, context: Context) {
  const { studentId, paymentId } = await context.params;
  const body = await request.json();
  const response = await authenticatedBackendFetch(
    `/api/fees/students/${studentId}/payments/${paymentId}/void`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    },
  );

  if (!response) {
    return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  }

  const text = await response.text();
  let result: unknown = {};
  if (text.trim()) {
    try { result = JSON.parse(text); } catch { result = { error: text }; }
  }
  return NextResponse.json(result, { status: response.status });
}
