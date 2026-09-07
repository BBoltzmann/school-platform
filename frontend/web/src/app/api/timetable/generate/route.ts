import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function POST(
  request: Request
) {
  const body =
    await request.json();

  const response =
    await authenticatedBackendFetch(
      "/api/timetable/generate",
      {
        method: "POST",
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

  let result: unknown = {};

  try {
    result =
      await response.json();
  } catch {
    // Keep empty object.
  }

  return NextResponse.json(
    result,
    {
      status: response.status,
    }
  );
}
