import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

import type {
  StaffAvailability,
  StaffMember,
} from "@/types/staff";

export async function getStaff(): Promise<StaffMember[]> {
  const response =
    await authenticatedBackendFetch(
      "/api/staff"
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load staff records."
    );
  }

  return response.json();
}

export async function getStaffMember(
  id: string
): Promise<StaffMember | null> {
  const response =
    await authenticatedBackendFetch(
      `/api/staff/${id}`
    );

  if (!response) {
    return null;
  }

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(
      "Unable to load staff member."
    );
  }

  return response.json();
}

export async function getStaffAvailability(
  staffId: string
): Promise<StaffAvailability[]> {
  const response =
    await authenticatedBackendFetch(
      `/api/staff/${staffId}/availability`
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load staff availability."
    );
  }

  return response.json();
}
