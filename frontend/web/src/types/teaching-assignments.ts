export type TeachingAssignment = {
  id: string;
  staffMemberId: string;
  staffNumber: string;
  staffName: string;

  academicSessionId: string;
  academicSessionName: string;

  classGroupId: string;
  classGroupName: string;

  academicLevelId: string;
  academicLevelName: string;
  usesCustomSubjectOffering: boolean;
  offeredSubjectIds: string[];

  subjectId: string;
  subjectName: string;

  isActive: boolean;
};

export type TeachingAssignmentSessionOption = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
};

export type TeachingAssignmentStaffOption = {
  id: string;
  staffNumber: string;
  name: string;
  jobTitle: string;
  department: string | null;
  employmentType: string;
};

export type TeachingAssignmentClassOption = {
  id: string;
  name: string;
  academicLevelId: string;
  academicLevelName: string;
  usesCustomSubjectOffering: boolean;
  offeredSubjectIds: string[];
};

export type TeachingAssignmentSubjectOption = {
  id: string;
  name: string;
};

export type TeachingAssignmentSetup = {
  currentSession: TeachingAssignmentSessionOption | null;
  teachingStaff: TeachingAssignmentStaffOption[];
  classes: TeachingAssignmentClassOption[];
  subjects: TeachingAssignmentSubjectOption[];
};
