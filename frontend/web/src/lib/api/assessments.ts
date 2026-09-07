import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

import type {
  AssessmentSetup,
} from "@/types/assessments";

export async function getAssessmentSetup(): Promise<AssessmentSetup> {
  const response =
    await authenticatedBackendFetch(
      "/api/assessments/setup"
    );

  if (!response) {
    throw new Error(
      "Unable to load assessment setup."
    );
  }

  if (!response.ok) {
    throw new Error(
      `Unable to load assessment setup. Backend returned ${response.status}.`
    );
  }

  return response.json();
}
