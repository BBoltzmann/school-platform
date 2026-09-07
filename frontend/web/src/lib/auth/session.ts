import { cookies } from "next/headers";

import type { SessionContext } from "@/types/session";

const API_URL =
  process.env.SCHOOL_PLATFORM_API_URL ??
  "http://localhost:5221";

export async function getSessionContext(): Promise<SessionContext | null> {
  const cookieStore = await cookies();

  const token =
    cookieStore.get("school_platform_token")?.value;

  if (!token) {
    return null;
  }

  try {
    const response = await fetch(
      `${API_URL}/api/tenant/context`,
      {
        method: "GET",
        headers: {
          Authorization: `Bearer ${token}`,
        },
        cache: "no-store",
      }
    );

    if (!response.ok) {
      return null;
    }

    return (await response.json()) as SessionContext;
  } catch {
    return null;
  }
}
