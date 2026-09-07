export type AdmissionApplication = {
  id: string;
  applicationNumber: string;

  firstName: string;
  middleName: string | null;
  lastName: string;
  dateOfBirth: string;
  gender: string;

  email: string | null;
  phone: string | null;
  religion: string | null;

  previousSchoolName: string | null;
  presentClass: string | null;

  guardianName: string | null;
  guardianHomeAddress: string | null;
  guardianOccupation: string | null;
  guardianPhone: string | null;
  guardianOfficeAddress: string | null;

  academicSessionId: string;
  academicSessionName: string;

  academicLevelId: string;
  academicLevelName: string;

  status: string;

  submittedAtUtc: string;
  reviewedAtUtc: string | null;
  decisionAtUtc: string | null;
  decisionNote: string | null;

  approvedStudentId: string | null;

  isActive: boolean;
};

export type AdmissionSessionOption = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
};

export type AdmissionLevelOption = {
  id: string;
  name: string;
  category: string | null;
  sortOrder: number;
};

export type AdmissionClassOption = {
  id: string;
  name: string;
  academicLevelId: string;
  academicLevelName: string;
  campusId: string;
};

export type AdmissionSetup = {
  currentSession: AdmissionSessionOption | null;
  levels: AdmissionLevelOption[];
  classes: AdmissionClassOption[];
};

export type AdmissionDocument = {
  id: string;
  documentType: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  uploadedAtUtc: string;
};

export type AdmissionRequirements = {
  passportPhotograph: boolean;
  birthCertificate: boolean;
  previousSchoolReport: boolean;
  medicalDocument: boolean;
  completed: number;
  total: number;
  percentage: number;
};
