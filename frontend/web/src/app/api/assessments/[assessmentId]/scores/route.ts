import {
  NextResponse,
} from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type RouteContext = {
  params: Promise<{
    assessmentId: string;
  }>;
};

async function proxy(
  response: Response | null
) {
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

export async function GET(
  _request: Request,
  context: RouteContext
) {
  const {
    assessmentId,
  } = await context.params;

  return proxy(
    await authenticatedBackendFetch(
      `/api/assessments/${assessmentId}/scores`
    )
  );
}

export async function PUT(
  request: Request,
  context: RouteContext
) {
  const {
    assessmentId,
  } = await context.params;

  const body =
    await request.json();

  return proxy(
    await authenticatedBackendFetch(
      `/api/assessments/${assessmentId}/scores`,
      {
        method: "PUT",
        headers: {
          "Content-Type":
            "application/json",
        },
        body: JSON.stringify(body),
      }
    )
  );
}
