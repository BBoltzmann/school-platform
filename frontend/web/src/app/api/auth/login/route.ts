import { NextResponse } from "next/server";

import { getBackendUrl } from "@/lib/api/backend-url";

export async function POST(request: Request) {
  let body;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ error: "Invalid login request." }, { status: 400 });
  }

  let apiUrl: string;
  try {
    apiUrl = getBackendUrl();
  } catch {
    console.error("Login configuration error: check SCHOOL_PLATFORM_API_URL.");
    return NextResponse.json(
      { error: "Sign-in is unavailable because the server is not configured correctly." },
      { status: 503 }
    );
  }

  try {
    const response = await fetch(
      `${apiUrl}/api/auth/login`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
        cache: "no-store",
        signal: AbortSignal.timeout(15_000),
      }
    );

    if (!response.ok) {
      console.error("Backend login failed", { origin: apiUrl, status: response.status });
      return NextResponse.json(
        {
          error: response.status === 401
            ? "Invalid email, password, or school."
            : "The sign-in service is unavailable. Please try again later.",
        },
        {
          status: response.status === 401 ? 401 : 502,
        }
      );
    }

    const result = await response.json();
    if (
      typeof result?.accessToken !== "string" || !result.accessToken ||
      typeof result.expiresAtUtc !== "string" ||
      !Number.isFinite(Date.parse(result.expiresAtUtc)) ||
      Date.parse(result.expiresAtUtc) <= Date.now()
    ) {
      throw new Error("Invalid backend login response");
    }

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
  } catch {
    console.error("Backend login unavailable or returned an invalid response", { origin: apiUrl });
    return NextResponse.json(
      { error: "The sign-in service is unavailable. Please try again later." },
      { status: 502 }
    );
  }
}
