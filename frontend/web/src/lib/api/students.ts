import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
import type {
  Student,
  StudentDetail,
  StudentSetup,
} from "@/types/students";

export async function getStudents(): Promise<Student[]> {
  const response =
    await authenticatedBackendFetch(
      "/api/students"
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load students."
    );
  }

  return response.json();
}

export async function getStudent(
  id: string
): Promise<StudentDetail | null> {
  const response =
    await authenticatedBackendFetch(
      `/api/students/${id}`
    );

  if (!response) {
    throw new Error(
      "Unable to load student."
    );
  }

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(
      "Unable to load student."
    );
  }

  return response.json();
}

export async function getStudentSetup(): Promise<StudentSetup> {
  const response =
    await authenticatedBackendFetch(
      "/api/students/setup"
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load student setup."
    );
  }

  return response.json();
}
