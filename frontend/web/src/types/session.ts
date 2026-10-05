export type SessionContext = {
  isAuthenticated: boolean;
  userId: string;
  email: string;
  tenantId: string;
  tenantSlug: string;
  tenantName: string;
  tenantBranding: {
    motto?: string | null;
    logoDataUrl?: string | null;
    iconDataUrl?: string | null;
    primaryColor: string;
    secondaryColor: string;
    accentColor: string;
  };
  membershipId: string;
  roles: string[];
  permissions: string[];
};
