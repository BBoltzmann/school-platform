import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
import { buildSubjectGradebookXlsx, type GradebookForExport } from "@/lib/reports/subject-gradebook-xlsx";

// Export is intentionally private and mirrors the existing gradebook endpoint's
// tenant/role/assignment checks. Never accept arbitrary file or tenant paths.
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export async function GET(request: Request) {
  const params = new URL(request.url).searchParams;
  const ids = ["academicTermId", "classGroupId", "subjectId"] as const;
  if (ids.some(id => !GUID.test(params.get(id) ?? ""))) {
    return Response.json({ error: "Select a valid term, class and subject." }, { status: 400 });
  }
  const query = new URLSearchParams(ids.map(id => [id, params.get(id)!]));
  let backendResponse: Response | null;
  try {
    backendResponse = await authenticatedBackendFetch(`/api/assessments/gradebook?${query}`);
  } catch {
    return Response.json({ error: "Gradebook service unavailable." }, { status: 503 });
  }
  if (!backendResponse) {
    return Response.json({ error: "Sign in to export the gradebook." }, { status: 401 });
  }
  if (!backendResponse.ok) {
    // Preserve the backend authorization status; do not leak other classes.
    return Response.json({ error: "Unable to export this gradebook." }, { status: backendResponse.status });
  }
  try {
    const gradebook = await backendResponse.json() as GradebookForExport;
    const workbook = buildSubjectGradebookXlsx(gradebook);
    return new Response(new Uint8Array(workbook), {
      status: 200,
      headers: {
        "Content-Type": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "Content-Disposition": 'attachment; filename="subject-gradebook.xlsx"',
        "Cache-Control": "private, no-store, max-age=0",
        "X-Content-Type-Options": "nosniff",
      },
    });
  } catch {
    return Response.json({ error: "Unable to generate gradebook export." }, { status: 500 });
  }
}
