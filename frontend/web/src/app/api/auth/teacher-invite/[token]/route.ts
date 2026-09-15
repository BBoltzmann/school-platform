import { NextResponse } from "next/server";
import { getBackendUrl } from "@/lib/api/backend-url";
export async function GET(_: Request, { params }: { params: Promise<{ token: string }> }) { const { token } = await params; try { const response = await fetch(`${getBackendUrl()}/api/auth/teacher-invite/${encodeURIComponent(token)}`, { cache: "no-store" }); return NextResponse.json(await response.json(), { status: response.status }); } catch { return NextResponse.json({ error: "Invitation service unavailable." }, { status: 503 }); } }
