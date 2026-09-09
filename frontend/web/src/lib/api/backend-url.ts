/** Server-side API origin shared by login, session lookup, and authenticated calls. */
export function getBackendUrl(): string {
  const configured = process.env.SCHOOL_PLATFORM_API_URL?.trim();

  if (!configured) {
    if (process.env.NODE_ENV === "production") {
      throw new Error("SCHOOL_PLATFORM_API_URL is required in production.");
    }
    return "http://localhost:5221";
  }

  const url = new URL(configured);
  if (
    !["http:", "https:"].includes(url.protocol) ||
    url.username || url.password || url.search || url.hash ||
    url.pathname.replace(/\/+$/, "") !== ""
  ) {
    throw new Error("SCHOOL_PLATFORM_API_URL must be an HTTP(S) origin without an API path.");
  }

  return url.origin;
}
