import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ academicTermId: string }> };

export async function POST(_request: Request, context: Context) {
  const { academicTermId } = await context.params;
  const response = await authenticatedBackendFetch(
    `/api/fees/terms/${academicTermId}/reconcile`,
    { method: "POST" }
  );

  if (!response) {
    return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  }

  let result: unknown = {};
  try {
    result = await response.json();
  } catch {
    result = { error: "The fee service returned an invalid response." };
  }

  return NextResponse.json(result, { status: response.status });
}
