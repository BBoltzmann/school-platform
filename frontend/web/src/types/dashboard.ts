export type DashboardPeriod = { id: string; name: string; startDate: string; endDate: string };
export type DashboardResponse = {
  generatedAtUtc: string;
  tenant: { id: string; slug: string; name: string };
  metrics: { totalStudents: number; totalStaff: number; pendingApplications: number; feesCollected: { amount: number; currency: string; period: string } };
  academic: { currentSession: DashboardPeriod | null; currentTerm: DashboardPeriod | null; admissionsEnabled: boolean; admissionsBlockedReason: string | null };
  actions: Array<{ code: string; priority: "high" | "normal" | string; count: number | null }>;
  recentActivity: Array<{ id: string; createdAtUtc: string; action: string; entityType: string; entityId: string | null }>;
  upcomingEvents: { availability: string; canCreate: boolean; items: Array<{ id: string; title: string; startsAt: string; location: string | null }> };
};
