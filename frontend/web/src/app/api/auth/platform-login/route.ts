import { NextResponse } from "next/server";
import { getBackendUrl } from "@/lib/api/backend-url";

export async function POST(request: Request) {
  const body = await request.json();
  const response = await fetch(`${getBackendUrl()}/api/auth/platform-login`, {
    method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body), cache: "no-store",
  });
  if (!response.ok) return NextResponse.json({ error: "Invalid platform credentials." }, { status: response.status });
  const result = await response.json();
  const next = NextResponse.json({ userId: result.userId, email: result.email, firstName: result.firstName, lastName: result.lastName, roles: result.roles, expiresAtUtc: result.expiresAtUtc });
  next.cookies.set("school_platform_token", result.accessToken, { httpOnly: true, secure: process.env.NODE_ENV === "production", sameSite: "lax", path: "/", expires: new Date(result.expiresAtUtc) });
  return next;
}
