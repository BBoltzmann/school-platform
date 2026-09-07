import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

import type {
  ClassSubjectRequirements,
  TimetablePlanningSetup,
  TimetableReadiness,
  TimetableTermOption,
} from "@/types/timetable";

async function requireResponse(
  response: Response | null,
  message: string
) {
  if (!response) {
    throw new Error(message);
  }

  if (!response.ok) {
    let detail = "";

    try {
      const body =
        await response.clone().json();

      detail =
        body?.error ??
        "";
    } catch {
      // Keep empty detail.
    }

    throw new Error(
      detail
        ? `${message} ${detail}`
        : message
    );
  }

  return response;
}

export async function getTimetableSetup(): Promise<TimetablePlanningSetup> {
  const response =
    await authenticatedBackendFetch(
      "/api/timetable/setup"
    );

  await requireResponse(
    response,
    "Unable to load timetable planning setup."
  );

  return response!.json();
}

export async function getClassSubjectRequirements(
  classGroupId: string
): Promise<ClassSubjectRequirements> {
  const response =
    await authenticatedBackendFetch(
      `/api/timetable/classes/${classGroupId}/requirements`
    );

  await requireResponse(
    response,
    "Unable to load class subject requirements."
  );

  return response!.json();
}

export async function getTimetableReadiness(): Promise<TimetableReadiness> {
  const response =
    await authenticatedBackendFetch(
      "/api/timetable/readiness"
    );

  await requireResponse(
    response,
    "Unable to load timetable readiness."
  );

  return response!.json();
}

export async function getTimetableTerms(): Promise<TimetableTermOption[]> {
  const response =
    await authenticatedBackendFetch(
      "/api/timetable/terms"
    );

  await requireResponse(
    response,
    "Unable to load academic terms."
  );

  return response!.json();
}
