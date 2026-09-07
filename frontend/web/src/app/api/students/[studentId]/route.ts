import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type RouteContext = {
  params: Promise<{
    studentId: string;
  }>;
};

export async function PATCH(
  request: Request,
  context: RouteContext
) {
  const { studentId } = await context.params;
  const body = await request.json();

  const response = await authenticatedBackendFetch(
    `/api/students/${studentId}`,
    {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    }
  );

  if (!response) {
    return NextResponse.json(
      { error: "Not authenticated." },
      { status: 401 }
    );
  }

  const result = await response.json();

  return NextResponse.json(result, {
    status: response.status,
  });
}
