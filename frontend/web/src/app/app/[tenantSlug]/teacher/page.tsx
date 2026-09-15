import { redirect } from "next/navigation";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
import { TeacherPortalView } from "@/components/teacher/teacher-portal-view";
import type { TeacherPortal } from "@/types/teacher";

export default async function TeacherPage({ params }: { params: Promise<{ tenantSlug: string }> }) {
  const { tenantSlug } = await params;
  const response = await authenticatedBackendFetch("/api/me/teacher-portal");
  if (!response || response.status === 401 || response.status === 403) redirect(`/app/${tenantSlug}/dashboard`);
  if (!response.ok) throw new Error("Teacher portal could not be loaded.");
  return <TeacherPortalView data={await response.json() as TeacherPortal} />;
}
