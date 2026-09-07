export type AcademicTerm = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  sortOrder: number;
};

export type AcademicSession = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  terms: AcademicTerm[];
};

export type Campus = {
  id: string;
  name: string;
  isActive: boolean;
};

export type AcademicLevel = {
  id: string;
  name: string;
  category: string;
  sortOrder: number;
  isActive: boolean;
  classCount: number;
};

export type ClassGroup = {
  id: string;
  name: string;
  campusId: string;
  academicLevelId: string;
  academicLevelName: string;
  isActive: boolean;
};

export type Subject = {
  id: string;
  name: string;
  code: string;
  category: string;
  isActive: boolean;
};

export type AcademicSetup = {
  currentSession: AcademicSession | null;
  campuses: Campus[];
  levels: AcademicLevel[];
  classes: ClassGroup[];
  subjects: Subject[];
};
