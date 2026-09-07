export type StudentEnrollment = {
  id: string;
  academicSessionId: string;
  academicSessionName: string;
  academicLevelId: string;
  academicLevelName: string;
  classGroupId: string;
  classGroupName: string;
  enrollmentDate: string;
  isCurrent: boolean;
};

export type Student = {
  id: string;
  admissionNumber: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  admissionDate: string;
  email: string | null;
  phone: string | null;
  status: string;
  isActive: boolean;
  currentEnrollment: StudentEnrollment | null;
};

export type StudentDetail = {
  id: string;
  admissionNumber: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  admissionDate: string;
  email: string | null;
  phone: string | null;
  status: string;
  isActive: boolean;
  enrollments: StudentEnrollment[];
};

export type StudentSessionOption = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
};

export type StudentLevelOption = {
  id: string;
  name: string;
  category: string;
  sortOrder: number;
};

export type StudentClassOption = {
  id: string;
  name: string;
  academicLevelId: string;
  academicLevelName: string;
  campusId: string;
};

export type StudentSetup = {
  currentSession: StudentSessionOption | null;
  levels: StudentLevelOption[];
  classes: StudentClassOption[];
};
