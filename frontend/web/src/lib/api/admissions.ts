import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";

import type {
  AdmissionApplication,
  AdmissionDocument,
  AdmissionRequirements,
  AdmissionSetup,
} from "@/types/admissions";

export async function getAdmissions(): Promise<
  AdmissionApplication[]
> {
  const response =
    await authenticatedBackendFetch(
      "/api/admissions"
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load admission applications."
    );
  }

  return response.json();
}

export async function getAdmission(
  id: string
): Promise<AdmissionApplication | null> {
  const response =
    await authenticatedBackendFetch(
      `/api/admissions/${id}`
    );

  if (!response) {
    return null;
  }

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(
      "Unable to load admission application."
    );
  }

  return response.json();
}

export async function getAdmissionSetup(): Promise<
  AdmissionSetup
> {
  const response =
    await authenticatedBackendFetch(
      "/api/admissions/setup"
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load admission setup."
    );
  }

  return response.json();
}

export async function getAdmissionDocuments(
  applicationId: string
): Promise<AdmissionDocument[]> {
  const response =
    await authenticatedBackendFetch(
      `/api/admissions/${applicationId}/documents`
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load admission documents."
    );
  }

  return response.json();
}

export async function getAdmissionRequirements(
  applicationId: string
): Promise<AdmissionRequirements> {
  const response =
    await authenticatedBackendFetch(
      `/api/admissions/${applicationId}/requirements`
    );

  if (!response || !response.ok) {
    throw new Error(
      "Unable to load admission requirements."
    );
  }

  return response.json();
}
