import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function GET(
  request: Request
) {
  const url =
    new URL(request.url);

  const academicTermId =
    url.searchParams.get(
      "academicTermId"
    );

  if (!academicTermId) {
    return NextResponse.json(
      {
        error:
          "Academic term is required.",
      },
      {
        status: 400,
      }
    );
  }

  const response =
    await authenticatedBackendFetch(
      `/api/fees/outstanding?academicTermId=${encodeURIComponent(
        academicTermId
      )}`
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

  const body = await response.text();
  let result: unknown;

  try {
    result = body ? JSON.parse(body) : {
      error: `Outstanding fees service returned HTTP ${response.status}.`,
    };
  } catch {
    result = {
      error: "Outstanding fees service returned an invalid response.",
    };
  }

  return NextResponse.json(
    result,
    {
      status:
        response.status,
    }
  );
}
