import { getBackendUrl } from "@/lib/api/backend-url";

export type SchoolBranding = {
  name?: string;
  motto?: string | null;
  logoDataUrl?: string | null;
  iconDataUrl?: string | null;
  primaryColor?: string;
  secondaryColor?: string;
  accentColor?: string;
};

export async function getSchoolBranding(schoolSlug: string): Promise<SchoolBranding | null> {
  try {
    const response = await fetch(`${getBackendUrl()}/api/public/schools/${encodeURIComponent(schoolSlug)}/branding`, { cache: "no-store" });
    return response.ok ? await response.json() as SchoolBranding : null;
  } catch {
    return null;
  }
}
