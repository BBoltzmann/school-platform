import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

import type {
  InventorySetup,
} from "@/types/inventory";

export async function getInventorySetup(): Promise<InventorySetup> {
  const response =
    await authenticatedBackendFetch(
      "/api/inventory/setup"
    );

  if (!response) {
    throw new Error(
      "Unable to load inventory. No authenticated backend response."
    );
  }

  if (!response.ok) {
    let detail = "";

    try {
      const body =
        await response.clone().json();

      detail =
        body?.error ?? "";
    } catch {
      // Ignore non-JSON response.
    }

    throw new Error(
      detail ||
        `Unable to load inventory. Backend returned ${response.status}.`
    );
  }

  return response.json();
}
