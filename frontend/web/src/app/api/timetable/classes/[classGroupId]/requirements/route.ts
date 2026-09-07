import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type RouteContext = {
  params: Promise<{
    classGroupId: string;
  }>;
};

export async function GET(
  _request: Request,
  context: RouteContext
) {
  const {
    classGroupId,
  } = await context.params;

  const response =
    await authenticatedBackendFetch(
      `/api/timetable/classes/${classGroupId}/requirements`
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

  let result: unknown = null;

  try {
    result =
      await response.json();
  } catch {
    // Keep null.
  }

  return NextResponse.json(
    result ?? {},
    {
      status: response.status,
    }
  );
}

export async function PUT(
  request: Request,
  context: RouteContext
) {
  const {
    classGroupId,
  } = await context.params;

  const body =
    await request.json();

  const response =
    await authenticatedBackendFetch(
      `/api/timetable/classes/${classGroupId}/requirements`,
      {
        method: "PUT",
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
        error:
          "Not authenticated.",
      },
      {
        status: 401,
      }
    );
  }

  let result: unknown = null;

  try {
    result =
      await response.json();
  } catch {
    // Keep null.
  }

  return NextResponse.json(
    result ?? {},
    {
      status: response.status,
    }
  );
}
