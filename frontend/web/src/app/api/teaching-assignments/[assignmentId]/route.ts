import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type RouteContext = {
  params: Promise<{
    assignmentId: string;
  }>;
};

export async function DELETE(
  _request: Request,
  context: RouteContext
) {
  const {
    assignmentId,
  } = await context.params;

  const response =
    await authenticatedBackendFetch(
      `/api/teaching-assignments/${assignmentId}`,
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

  let result: unknown = null;

  try {
    result =
      await response.json();
  } catch {
    // Keep empty response.
  }

  return NextResponse.json(
    result ?? {},
    {
      status: response.status,
    }
  );
}
