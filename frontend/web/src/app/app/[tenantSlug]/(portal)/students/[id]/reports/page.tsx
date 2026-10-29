import { StudentReportPreview } from "@/components/reports/student-report-preview";

export default async function StudentReportPage({
  params,
}: {
  params: Promise<{ tenantSlug: string; id: string }>;
}) {
  const { tenantSlug, id } = await params;
  return <StudentReportPreview
    studentId={id}
    backHref={`/app/${tenantSlug}/students/${id}`}
    backLabel="Back to Student Profile"
  />;
}
