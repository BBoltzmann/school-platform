import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = { params: Promise<{ id: string }> };

export async function POST(request: Request, context: Context) {
  const { id } = await context.params;
  const url = new URL(request.url);
  const response = await authenticatedBackendFetch(
    `/api/academics/classes/${id}/parallel-subject-groups/reset${url.search}`,
    { method: "POST" }
  );

  if (!response) {
    return NextResponse.json({ error: "Not authenticated." }, { status: 401 });
  }

  const body = await response.text();
  let result: unknown = {};
  try {
    result = body ? JSON.parse(body) : {};
  } catch {
    result = { error: "The timetable service returned an invalid response." };
  }

  return NextResponse.json(result, { status: response.status });
}
