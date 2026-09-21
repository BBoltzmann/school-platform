export type SessionContext = {
  isAuthenticated: boolean;
  userId: string;
  email: string;
  tenantId: string;
  tenantSlug: string;
  tenantName: string;
  membershipId: string;
  roles: string[];
  permissions: string[];
};
