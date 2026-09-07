import { NextResponse } from "next/server";

const API_URL =
  process.env.SCHOOL_PLATFORM_API_URL ??
  "http://localhost:5221";

export async function POST(request: Request) {
  const body = await request.json();

  const response = await fetch(
    `${API_URL}/api/auth/login`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
      cache: "no-store",
    }
  );

  if (!response.ok) {
    return NextResponse.json(
      {
        error: "Invalid email, password, or school.",
      },
      {
        status: response.status,
      }
    );
  }

  const result = await response.json();

  const nextResponse = NextResponse.json({
    userId: result.userId,
    tenantId: result.tenantId,
    membershipId: result.membershipId,
    email: result.email,
    firstName: result.firstName,
    lastName: result.lastName,
    roles: result.roles,
    permissions: result.permissions,
    expiresAtUtc: result.expiresAtUtc,
  });

  nextResponse.cookies.set(
    "school_platform_token",
    result.accessToken,
    {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      expires: new Date(result.expiresAtUtc),
    }
  );

  return nextResponse;
}
