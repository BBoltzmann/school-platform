import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = {
  params: Promise<{
    itemId: string;
  }>;
};

export async function POST(
  request: Request,
  context: Context
) {
  const {
    itemId,
  } = await context.params;

  const body =
    await request.json();

  const response =
    await authenticatedBackendFetch(
      `/api/inventory/items/${itemId}/variants`,
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
        error: "Not authenticated.",
      },
      {
        status: 401,
      }
    );
  }

  const result =
    await response.json();

  return NextResponse.json(
    result,
    {
      status: response.status,
    }
  );
}
