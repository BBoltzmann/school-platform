import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
import type { AcademicSetup } from "@/types/academics";

export async function getAcademicSetup(): Promise<AcademicSetup> {
  const response =
    await authenticatedBackendFetch(
      "/api/academics/setup"
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load academic setup."
    );
  }

  return response.json();
}
