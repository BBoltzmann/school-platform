import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = {
  params: Promise<{
    structureId: string;
  }>;
};

export async function GET(
  _request: Request,
  context: Context
) {
  const { structureId } = await context.params;
  return proxy(`/api/fees/structures/${structureId}/students`);
}

export async function PUT(
  request: Request,
  context: Context
) {
  const { structureId } = await context.params;
  const body = await request.json();

  return proxy(
    `/api/fees/structures/${structureId}/students`,
    {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    }
  );
}

async function proxy(
  path: string,
  options: RequestInit = {}
) {
  const response = await authenticatedBackendFetch(path, options);

  if (!response) {
    return NextResponse.json(
      { error: "Not authenticated." },
      { status: 401 }
    );
  }

  const body = await response.text();
  let result: unknown = {};

  if (body.trim()) {
    try {
      result = JSON.parse(body);
    } catch {
      result = {
        error: response.ok
          ? "The fee service returned an invalid response."
          : `Fee service request failed (${response.status}).`,
      };
    }
  } else if (!response.ok) {
    result = {
      error: `Fee service request failed (${response.status}).`,
    };
  }

  return NextResponse.json(result, {
    status: response.status,
  });
}
