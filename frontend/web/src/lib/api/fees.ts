import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

import type {
  FeesSetup,
} from "@/types/fees";

export async function getFeesSetup(): Promise<FeesSetup> {
  const response =
    await authenticatedBackendFetch(
      "/api/fees/setup"
    );

  if (!response) {
    throw new Error(
      "Unable to load fees management."
    );
  }

  if (!response.ok) {
    throw new Error(
      `Unable to load fees management. Backend returned ${response.status}.`
    );
  }

  return response.json();
}
