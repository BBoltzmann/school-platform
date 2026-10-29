export type SchoolPortal = "admin" | "staff";

export function schoolPath(schoolSlug: string): string {
  return `/schools/${encodeURIComponent(schoolSlug)}`;
}

export function schoolLoginPath(schoolSlug: string, portal: SchoolPortal = "admin"): string {
  return `${schoolPath(schoolSlug)}/${portal}/login`;
}

export function schoolAdminLoginPath(schoolSlug: string, query?: Record<string, string>): string {
  const path = schoolLoginPath(schoolSlug, "admin");
  if (!query) return path;
  const params = new URLSearchParams(query);
  return `${path}?${params}`;
}

export function schoolStaffLoginPath(schoolSlug: string): string {
  return schoolLoginPath(schoolSlug, "staff");
}

/**
 * Choose a landing page from authenticated tenant roles, never from the login
 * link the visitor happened to open. Authorization is still enforced by the API.
 */
export function schoolHomeForRoles(schoolSlug: string, roles: string[]): string | null {
  const normalized = roles.map(role => role.trim().toLowerCase());
  if (normalized.length === 0) return null;

  const administrator = normalized.some(role => role.includes("administrator") || role === "admin" || role === "school admin");
  const teacher = normalized.includes("teacher");
  const section = teacher && !administrator ? "teacher" : "dashboard";
  return `/app/${encodeURIComponent(schoolSlug)}/${section}`;
}
