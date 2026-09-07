import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function GET() {
  const response =
    await authenticatedBackendFetch(
      "/api/academics/setup"
    );

  if (!response) {
    return NextResponse.json(
      { error: "Not authenticated." },
      { status: 401 }
    );
  }

  const body = await response.json();

  return NextResponse.json(body, {
    status: response.status,
  });
}
