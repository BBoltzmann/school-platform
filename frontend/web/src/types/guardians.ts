export type Guardian = {
  id: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
  email: string | null;
  phone: string;
  alternatePhone: string | null;
  occupation: string | null;
  address: string | null;
  isActive: boolean;
};

export type StudentGuardian = {
  id: string;
  studentId: string;
  guardian: Guardian;
  relationship: string;
  isPrimaryContact: boolean;
  isEmergencyContact: boolean;
  canPickUpStudent: boolean;
  livesWithStudent: boolean;
  isActive: boolean;
};
