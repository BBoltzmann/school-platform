import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = {
  params: Promise<{
    studentId: string;
  }>;
};

export async function GET(
  request: Request,
  context: Context
) {
  const {
    studentId,
  } = await context.params;

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
      `/api/fees/students/${studentId}/account?academicTermId=${encodeURIComponent(
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

  const result =
    await response.json();

  return NextResponse.json(
    result,
    {
      status:
        response.status,
    }
  );
}
