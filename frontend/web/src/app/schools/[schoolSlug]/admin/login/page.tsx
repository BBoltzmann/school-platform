import { notFound } from "next/navigation";
import { SchoolPortalLogin } from "@/components/auth/school-portal-login";
import { getSchoolBranding } from "@/lib/school-branding";

export default async function SchoolAdminLoginPage({ params, searchParams }: { params: Promise<{ schoolSlug: string }>; searchParams: Promise<{ setup?: string }> }) {
  const { schoolSlug } = await params;
  const branding = await getSchoolBranding(schoolSlug);
  if (!branding) notFound();
  return <SchoolPortalLogin schoolSlug={schoolSlug} portal="admin" branding={branding} setup={(await searchParams).setup} />;
}
