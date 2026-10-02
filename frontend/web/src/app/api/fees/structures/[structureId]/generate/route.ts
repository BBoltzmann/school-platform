import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = {
  params: Promise<{
    structureId: string;
  }>;
};

export async function POST(
  _request: Request,
  context: Context
) {
  const {
    structureId,
  } = await context.params;

  const response =
    await authenticatedBackendFetch(
      `/api/fees/structures/${structureId}/generate`,
      {
        method: "POST",
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

  const body = await response.text();
  let result: unknown = {};
  if (body.trim()) {
    try {
      result = JSON.parse(body);
    } catch {
      result = {
        error: `Fee service request failed (HTTP ${response.status}).`,
      };
    }
  } else if (!response.ok) {
    result = {
      error: `Fee service request failed (HTTP ${response.status}).`,
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
