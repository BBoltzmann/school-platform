import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type RouteContext = {
  params: Promise<{
    id: string;
    documentId: string;
  }>;
};

export async function GET(
  _request: Request,
  context: RouteContext
) {
  const {
    id,
    documentId,
  } = await context.params;

  const response =
    await authenticatedBackendFetch(
      `/api/admissions/${id}/documents/${documentId}/download`
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

  if (!response.ok) {
    let error =
      "Unable to download document.";

    try {
      const result =
        await response.json();

      error =
        result.error ?? error;
    } catch {
      // Keep default error.
    }

    return NextResponse.json(
      { error },
      {
        status: response.status,
      }
    );
  }

  const headers =
    new Headers();

  const contentType =
    response.headers.get(
      "content-type"
    );

  const disposition =
    response.headers.get(
      "content-disposition"
    );

  const length =
    response.headers.get(
      "content-length"
    );

  if (contentType) {
    headers.set(
      "Content-Type",
      contentType
    );
  }

  if (disposition) {
    headers.set(
      "Content-Disposition",
      disposition
    );
  }

  if (length) {
    headers.set(
      "Content-Length",
      length
    );
  }

  return new Response(
    response.body,
    {
      status: 200,
      headers,
    }
  );
}
