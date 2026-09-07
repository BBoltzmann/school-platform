import { NextResponse } from "next/server";

import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

export async function GET(request: Request) {
  const url = new URL(request.url);

  const search =
    url.searchParams.get("search") ?? "";

  const response =
    await authenticatedBackendFetch(
      `/api/guardians?search=${encodeURIComponent(search)}`
    );

  if (!response) {
    return NextResponse.json(
      { error: "Not authenticated." },
      { status: 401 }
    );
  }

  const result =
    await response.json();

  return NextResponse.json(result, {
    status: response.status,
  });
}
