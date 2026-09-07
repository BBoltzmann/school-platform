import {
  NextResponse,
} from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type RouteContext = {
  params: Promise<{
    assessmentId: string;
  }>;
};

export async function PATCH(
  request: Request,
  context: RouteContext
) {
  const {
    assessmentId,
  } = await context.params;

  const body =
    await request.json();

  const response =
    await authenticatedBackendFetch(
      `/api/assessments/${assessmentId}`,
      {
        method: "PATCH",
        headers: {
          "Content-Type":
            "application/json",
        },
        body: JSON.stringify(body),
      }
    );

  if (!response) {
    return NextResponse.json(
      {
        error: "Not authenticated.",
      },
      {
        status: 401,
      }
    );
  }

  let result: unknown = {};

  try {
    result =
      await response.json();
  } catch {
    // Empty response.
  }

  return NextResponse.json(
    result,
    {
      status: response.status,
    }
  );
}

export async function DELETE(
  _request: Request,
  context: RouteContext
) {
  const {
    assessmentId,
  } = await context.params;

  const response =
    await authenticatedBackendFetch(
      `/api/assessments/${assessmentId}`,
      {
        method: "DELETE",
      }
    );

  if (!response) {
    return NextResponse.json(
      {
        error: "Not authenticated.",
      },
      {
        status: 401,
      }
    );
  }

  if (response.status === 204) {
    return new NextResponse(
      null,
      {
        status: 204,
      }
    );
  }

  let result: unknown = {};

  try {
    result =
      await response.json();
  } catch {
    // Empty response.
  }

  return NextResponse.json(
    result,
    {
      status: response.status,
    }
  );
}
