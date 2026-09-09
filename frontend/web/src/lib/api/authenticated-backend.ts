import { cookies } from "next/headers";

import { getBackendUrl } from "@/lib/api/backend-url";

export async function authenticatedBackendFetch(
  path: string,
  options: RequestInit = {}
) {
  const cookieStore = await cookies();

  const token =
    cookieStore.get("school_platform_token")?.value;

  if (!token) {
    return null;
  }

  return fetch(`${getBackendUrl()}${path}`, {
    ...options,
    headers: {
      ...options.headers,
      Authorization: `Bearer ${token}`,
    },
    cache: "no-store",
  });
}
