import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

import type {
  TeachingAssignment,
  TeachingAssignmentSetup,
} from "@/types/teaching-assignments";

export async function getTeachingAssignmentSetup(): Promise<TeachingAssignmentSetup> {
  const response =
    await authenticatedBackendFetch(
      "/api/teaching-assignments/setup"
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load teaching assignment setup."
    );
  }

  return response.json();
}

export async function getStaffTeachingAssignments(
  staffId: string
): Promise<TeachingAssignment[]> {
  const response =
    await authenticatedBackendFetch(
      `/api/staff/${staffId}/teaching-assignments`
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load teaching assignments."
    );
  }

  return response.json();
}
