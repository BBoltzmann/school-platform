import { redirect } from "next/navigation";
import { getSessionContext } from "@/lib/auth/session";
import { ParentMidtermReport } from "@/components/reports/parent-midterm-report";

export const metadata = { title: "Midterm Progress Report" };

export default async function ParentSummaryPage({ params, searchParams }: {
  params: Promise<{ tenantSlug: string; studentId: string }>;
  searchParams: Promise<{ academicTermId?: string; source?: string }>;
}) {
  const { tenantSlug, studentId } = await params;
  const { academicTermId, source } = await searchParams;
  const session = await getSessionContext();
  if (!session || session.tenantSlug !== tenantSlug) {
    redirect(`/schools/${encodeURIComponent(tenantSlug)}/admin/login`);
  }

  const backHref = source === "teacher"
    ? `/app/${tenantSlug}/teacher/students/${studentId}/reports`
    : `/app/${tenantSlug}/students/${studentId}/reports`;

  return <ParentMidtermReport studentId={studentId} termId={academicTermId ?? ""}
    schoolName={session.tenantName} logoDataUrl={session.tenantBranding.logoDataUrl}
    backHref={backHref} />;
}
