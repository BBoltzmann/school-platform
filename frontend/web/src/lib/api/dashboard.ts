import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
import type { DashboardResponse } from "@/types/dashboard";

export async function getDashboard(): Promise<DashboardResponse> {
  const response = await authenticatedBackendFetch("/api/dashboard");
  if (!response || !response.ok) throw new Error("Dashboard data could not be loaded.");
  return response.json() as Promise<DashboardResponse>;
}
