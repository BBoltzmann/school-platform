import { NextResponse } from "next/server";
import { getBackendUrl } from "@/lib/api/backend-url";
export async function POST(request: Request) { try { const body = await request.json(); const response = await fetch(`${getBackendUrl()}/api/auth/teacher-invite/activate`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body), cache: "no-store" }); return NextResponse.json(await response.json(), { status: response.status }); } catch { return NextResponse.json({ error: "Invitation service unavailable." }, { status: 503 }); } }
