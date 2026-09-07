import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
import type {
  Guardian,
  StudentGuardian,
} from "@/types/guardians";

export async function getStudentGuardians(
  studentId: string
): Promise<StudentGuardian[]> {
  const response =
    await authenticatedBackendFetch(
      `/api/students/${studentId}/guardians`
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load student guardians."
    );
  }

  return response.json();
}

export async function searchGuardians(
  search: string
): Promise<Guardian[]> {
  const response =
    await authenticatedBackendFetch(
      `/api/guardians?search=${encodeURIComponent(search)}`
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to search guardians."
    );
  }

  return response.json();
}
