import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function PUT(
  request: Request
) {
  const body =
    await request.json();

  const response =
    await authenticatedBackendFetch(
      "/api/timetable/settings",
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
    // Keep null response.
  }

  return NextResponse.json(
    result ?? {},
    {
      status: response.status,
    }
  );
}
