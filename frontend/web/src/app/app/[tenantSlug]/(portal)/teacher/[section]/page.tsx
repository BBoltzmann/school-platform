import { redirect } from "next/navigation";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
import { TeacherPortalView } from "@/components/teacher/teacher-portal-view";
import type { TeacherPortal } from "@/types/teacher";
import { tenantDisplayName } from "@/lib/tenant-display-name";
import { getSessionContext } from "@/lib/auth/session";
import { getAssessmentSetup } from "@/lib/api/assessments";
import { AssessmentWorkspace } from "@/components/assessments/assessment-workspace";

export default async function TeacherSectionPage({ params }: { params: Promise<{ tenantSlug: string; section: string }> }) {
  const { tenantSlug, section } = await params;
  const response = await authenticatedBackendFetch("/api/me/teacher-portal");
  if (!response || response.status === 401 || response.status === 403) redirect(`/app/${tenantSlug}/dashboard`);
  if (!response.ok) throw new Error("Teacher portal could not be loaded.");
  if (section === "assessments") {
    const setup = await getAssessmentSetup();
    if (!setup.currentSession) {
      return <div className="space-y-3"><h1 className="text-2xl font-bold">My Assessments</h1><p>The administrator must configure a current academic session before assessments can be entered.</p></div>;
    }
    // The setup API returns only the current teacher's active assignments.
    const assigned = (setup as typeof setup & {
      assignments?: { classGroupId: string; subjectId: string }[];
    }).assignments ?? [];
    return <div className="space-y-6"><div><h1 className="text-2xl font-bold tracking-tight">My Assessments</h1><p className="mt-1 text-sm text-muted-foreground">Create assessments and enter scores for your assigned subjects. Scores share the administrator gradebook.</p></div><AssessmentWorkspace setup={setup} allowedPairs={assigned} allowDelete={false} /></div>;
  }
  const session = await getSessionContext();
  return <TeacherPortalView data={await response.json() as TeacherPortal} section={section} schoolName={session?.tenantName ?? tenantDisplayName(tenantSlug)} tenantSlug={tenantSlug} />;
}
