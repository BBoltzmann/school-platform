export type StaffMember = {
  id: string;
  staffNumber: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
  gender: string;
  dateOfBirth: string | null;
  email: string | null;
  phone: string;
  address: string | null;
  employmentDate: string;
  jobTitle: string;
  department: string | null;
  employmentType: string;
  isTeachingStaff: boolean;
  status: string;
  isActive: boolean;
  userId: string | null;
};

export type StaffAvailability = {
  id: string;
  dayOfWeek: number | string;
  startTime: string;
  endTime: string;
  isActive: boolean;
};

export type WorkingDayInput = {
  dayOfWeek: number;
  label: string;
  enabled: boolean;
  startTime: string;
  endTime: string;
};
