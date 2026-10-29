import { NextResponse } from "next/server";
import { getBackendUrl } from "@/lib/api/backend-url";
import { PASSWORD_DESCRIPTION, validPassword } from "@/lib/auth/recovery";

export async function POST(request: Request) {
  try {
    const body = await request.json();
    if (typeof body?.password !== "string" || !validPassword(body.password)) {
      return NextResponse.json({ error: PASSWORD_DESCRIPTION, code: "password_policy" }, { status: 400 });
    }
    const response = await fetch(`${getBackendUrl()}/api/auth/teacher-invite/activate`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body), cache: "no-store",
    });
    return NextResponse.json(await response.json(), { status: response.status });
  } catch {
    return NextResponse.json({ error: "Invitation service unavailable." }, { status: 503 });
  }
}
