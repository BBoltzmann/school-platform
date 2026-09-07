import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type RouteContext = {
  params: Promise<{
    id: string;
    documentId: string;
  }>;
};

export async function DELETE(
  _request: Request,
  context: RouteContext
) {
  const {
    id,
    documentId,
  } = await context.params;

  const response =
    await authenticatedBackendFetch(
      `/api/admissions/${id}/documents/${documentId}`,
      {
        method: "DELETE",
      }
    );

  if (!response) {
    return NextResponse.json(
      {
        error:
          "Not authenticated.",
      },
      {
        status: 401,
      }
    );
  }

  if (response.status === 204) {
    return new Response(null, {
      status: 204,
    });
  }

  const result =
    await response.json();

  return NextResponse.json(
    result,
    {
      status: response.status,
    }
  );
}
