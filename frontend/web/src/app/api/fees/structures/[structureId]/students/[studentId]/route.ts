import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

type Context = {
  params: Promise<{
    structureId: string;
    studentId: string;
  }>;
};

export async function DELETE(
  _request: Request,
  context: Context
) {
  const { structureId, studentId } = await context.params;
  const response = await authenticatedBackendFetch(
    `/api/fees/structures/${structureId}/students/${studentId}`,
    { method: "DELETE" }
  );

  if (!response) {
    return NextResponse.json(
      { error: "Not authenticated." },
      { status: 401 }
    );
  }

  const result = await response.json();
  return NextResponse.json(result, {
    status: response.status,
  });
}
