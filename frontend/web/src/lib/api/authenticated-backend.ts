import { cookies } from "next/headers";

const API_URL =
  process.env.SCHOOL_PLATFORM_API_URL ??
  "http://localhost:5221";

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

  return fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      ...options.headers,
      Authorization: `Bearer ${token}`,
    },
    cache: "no-store",
  });
}
