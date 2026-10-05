import { NextResponse } from "next/server";
import { getBackendUrl } from "@/lib/api/backend-url";
import { publicAuthPost } from "@/lib/api/public-auth";

export async function GET(request: Request) {
  const token = new URL(request.url).searchParams.get("token") ?? "";
  try {
    const response = await fetch(`${getBackendUrl()}/api/auth/setup?token=${encodeURIComponent(token)}`, { cache: "no-store", signal: AbortSignal.timeout(15_000) });
    const body = await response.text();
    return new NextResponse(body, { status: response.status, headers: { "Content-Type": response.headers.get("content-type") ?? "application/json" } });
  } catch {
    return NextResponse.json({ error: "The authentication service is unavailable. Please try again later." }, { status: 502 });
  }
}

export async function POST(request: Request) {
  return publicAuthPost(request, "setup");
}
