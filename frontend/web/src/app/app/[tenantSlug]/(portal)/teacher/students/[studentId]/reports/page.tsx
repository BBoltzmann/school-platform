import { StudentReportPreview } from "@/components/reports/student-report-preview";

export default async function TeacherStudentReportPage({
  params,
}: {
  params: Promise<{ tenantSlug: string; studentId: string }>;
}) {
  const { tenantSlug, studentId } = await params;
  return <StudentReportPreview
    studentId={studentId}
    backHref={`/app/${tenantSlug}/teacher/students`}
    backLabel="Back to My Students"
  />;
}
