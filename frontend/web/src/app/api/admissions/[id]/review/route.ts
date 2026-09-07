import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type RouteContext = {
  params: Promise<{
    id: string;
  }>;
};

export async function PATCH(
  _request: Request,
  context: RouteContext
) {
  const { id } = await context.params;

  const response =
    await authenticatedBackendFetch(
      `/api/admissions/${id}/review`,
      {
        method: "PATCH",
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
