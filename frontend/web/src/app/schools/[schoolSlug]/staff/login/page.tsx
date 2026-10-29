import { notFound } from "next/navigation";
import { SchoolPortalLogin } from "@/components/auth/school-portal-login";
import { getSchoolBranding } from "@/lib/school-branding";

export default async function SchoolStaffLoginPage({ params }: { params: Promise<{ schoolSlug: string }> }) {
  const { schoolSlug } = await params;
  const branding = await getSchoolBranding(schoolSlug);
  if (!branding) notFound();
  return <SchoolPortalLogin schoolSlug={schoolSlug} portal="staff" branding={branding} />;
}
