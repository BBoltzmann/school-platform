import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type RouteContext = {
  params: Promise<{
    academicTermId: string;
  }>;
};

export async function GET(
  _request: Request,
  context: RouteContext
) {
  const {
    academicTermId,
  } = await context.params;

  const response =
    await authenticatedBackendFetch(
      `/api/timetable/generated/${academicTermId}`
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

  if (response.status === 404) {
    return NextResponse.json(
      {
        error:
          "No timetable has been generated for this term.",
      },
      {
        status: 404,
      }
    );
  }

  let result: unknown = {};

  try {
    result =
      await response.json();
  } catch {
    // Keep empty result.
  }

  return NextResponse.json(
    result,
    {
      status: response.status,
    }
  );
}
