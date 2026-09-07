import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = {
  params: Promise<{
    inventoryListId: string;
  }>;
};

export async function GET(
  _request: Request,
  context: Context
) {
  const {
    inventoryListId,
  } = await context.params;

  const response =
    await authenticatedBackendFetch(
      `/api/inventory/lists/${inventoryListId}/demand`
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
