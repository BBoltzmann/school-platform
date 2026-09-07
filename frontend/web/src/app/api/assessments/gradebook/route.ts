import {
  NextResponse,
} from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function GET(
  request: Request
) {
  const url =
    new URL(request.url);

  const response =
    await authenticatedBackendFetch(
      `/api/assessments/gradebook${url.search}`
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
