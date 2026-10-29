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
