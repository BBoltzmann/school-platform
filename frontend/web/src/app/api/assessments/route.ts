import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

async function proxyResponse(
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
  request: Request
) {
  const url =
    new URL(request.url);

  const response =
    await authenticatedBackendFetch(
      `/api/assessments${url.search}`
    );

  return proxyResponse(
    response
  );
}

export async function POST(
  request: Request
) {
  const body =
    await request.json();

  const response =
    await authenticatedBackendFetch(
      "/api/assessments",
      {
        method: "POST",
        headers: {
          "Content-Type":
            "application/json",
        },
        body: JSON.stringify(body),
      }
    );

  return proxyResponse(
    response
  );
}
