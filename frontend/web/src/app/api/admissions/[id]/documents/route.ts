import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type RouteContext = {
  params: Promise<{
    id: string;
  }>;
};

export async function POST(
  request: Request,
  context: RouteContext
) {
  const { id } = await context.params;

  const formData =
    await request.formData();

  const response =
    await authenticatedBackendFetch(
      `/api/admissions/${id}/documents`,
      {
        method: "POST",
        body: formData,
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

  const contentType =
    response.headers.get(
      "content-type"
    );

  if (
    contentType?.includes(
      "application/json"
    )
  ) {
    const result =
      await response.json();

    return NextResponse.json(
      result,
      {
        status: response.status,
      }
    );
  }

  return NextResponse.json(
    {
      error:
        "Unexpected response from the document service.",
    },
    {
      status:
        response.status >= 400
          ? response.status
          : 500,
    }
  );
}
