import { NextResponse } from "next/server";
import { getBackendUrl } from "@/lib/api/backend-url";

export async function GET(_: Request, { params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const response = await fetch(`${getBackendUrl()}/api/public/schools/${encodeURIComponent(slug)}/branding`, { cache: "no-store" });
  return new NextResponse(await response.text(), { status: response.status, headers: { "Content-Type": response.headers.get("content-type") ?? "application/json" } });
}
