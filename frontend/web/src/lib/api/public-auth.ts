import { NextResponse } from "next/server";
import { getBackendUrl } from "@/lib/api/backend-url";
import { RECOVERY_MESSAGE, SIGNUP_MESSAGE } from "@/lib/auth/recovery";

type Action = "direct-password-reset" | "forgot-password" | "reset-password" | "signup";
const errors: Record<Action, string> = {
  "direct-password-reset": "Unable to change password. Check the recovery details and password requirements.",
  "forgot-password": "Check your email address and school slug.",
  "reset-password": "This reset link is invalid or expired, or the password does not meet the requirements. Request a new link if needed.",
  signup: "Check the school details and password requirements.",
};

export async function publicAuthPost(request: Request, action: Action) {
  if (action === "signup" && process.env.ALLOW_PUBLIC_SCHOOL_SIGNUP !== "true") {
    return NextResponse.json({ error: "Public school creation is unavailable." }, { status: 404 });
  }
  const origin = request.headers.get("origin");
  if (origin && origin !== new URL(request.url).origin) {
    return NextResponse.json({ error: "Invalid request origin." }, { status: 403 });
  }
  let body;
  try {
    body = await request.json();
    if (!body || typeof body !== "object" || Array.isArray(body)) throw new Error("Invalid request");
  } catch {
    return NextResponse.json({ error: errors[action] }, { status: 400 });
  }
  let apiUrl;
  try {
    apiUrl = getBackendUrl();
  } catch {
    console.error("Authentication configuration error: check SCHOOL_PLATFORM_API_URL.");
    return NextResponse.json({ error: "The authentication service is unavailable. Please try again later." }, { status: 503 });
  }
  try {
    const response = await fetch(`${apiUrl}/api/auth/${action}`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body), cache: "no-store", signal: AbortSignal.timeout(15_000),
    });
    if (!response.ok) {
      const status = [400, 404, 409, 429].includes(response.status) ? response.status : 502;
      const error = status === 429 ? "Too many attempts. Please try again later."
        : status === 409 && action === "signup" ? "This school slug is unavailable. Choose a different slug."
        : status === 404 && action === "direct-password-reset" ? "Password recovery is currently unavailable. Contact your administrator."
        : status === 404 && action === "signup" ? "Public school creation is unavailable."
        : status === 400 ? errors[action]
        : "The authentication service is unavailable. Please try again later.";
      return NextResponse.json({ error }, { status });
    }
    if (action === "reset-password" || action === "direct-password-reset") {
      const result = await response.json();
      if (typeof result?.tenantSlug !== "string" || !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(result.tenantSlug)) {
        throw new Error("Invalid reset response");
      }
      const nextResponse = NextResponse.json({ tenantSlug: result.tenantSlug });
      // Require a fresh login after recovery, including when another user was signed in.
      nextResponse.cookies.set("school_platform_token", "", {
        httpOnly: true, secure: process.env.NODE_ENV === "production", sameSite: "lax", path: "/", maxAge: 0,
      });
      return nextResponse;
    }
    return NextResponse.json({ message: action === "forgot-password" ? RECOVERY_MESSAGE : SIGNUP_MESSAGE });
  } catch {
    console.error("Authentication backend request failed", { action });
    return NextResponse.json({ error: "The authentication service is unavailable. Please try again later." }, { status: 502 });
  }
}
