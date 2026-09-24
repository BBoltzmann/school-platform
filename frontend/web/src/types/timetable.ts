export type TimetableSessionOption = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
};

export type TimetableTermOption = {
  id: string;
  name: string;
};

export type TimetableClassOption = {
  id: string;
  name: string;
  academicLevelId: string;
  academicLevelName: string;
};

export type TimetableSubjectOption = {
  id: string;
  name: string;
};

export type TimetableNonTeachingBlock = {
  id: string;
  name: string;
  startTime: string;
  endTime: string;
  sortOrder: number;
};

export type TimetableDay = {
  id: string;
  dayOfWeek: number | string;
  startTime: string;
  endTime: string;
  nonTeachingBlocks: TimetableNonTeachingBlock[];
};

export type TimetableSettings = {
  id: string;
  academicSessionId: string;
  academicSessionName: string;
  periodDurationMinutes: number;
  days: TimetableDay[];
};

export type TimetablePlanningSetup = {
  currentSession: TimetableSessionOption | null;
  settings: TimetableSettings | null;
  classes: TimetableClassOption[];
  subjects: TimetableSubjectOption[];
};

export type ClassSubjectRequirement = {
  id: string;
  subjectId: string;
  subjectName: string;
  periodsPerWeek: number;
};

export type ClassSubjectRequirements = {
  classGroupId: string;
  classGroupName: string;
  academicLevelId: string;
  academicLevelName: string;
  academicSessionId: string;
  academicSessionName: string;
  totalPeriodsPerWeek: number;
  effectiveTimetablePeriods: number;
  parallelSavings: number;
  requirements: ClassSubjectRequirement[];
};

export type TimetableBlockInput = {
  clientId: string;
  name: string;
  startTime: string;
  endTime: string;
  parallelSubjectGroupId?: string | null;
  parallelOccurrenceId?: string | null;
  parallelDisplayName?: string | null;
  parallelMembers?: { subjectId: string; staffMemberId: string; subjectName: string; staffName: string }[];
};

export type TimetableDayInput = {
  dayOfWeek: number;
  label: string;
  enabled: boolean;
  startTime: string;
  endTime: string;
  blocks: TimetableBlockInput[];
};

export type TimetableReadinessIssue = {
  code: string;
  severity: string;
  classGroupId: string | null;
  classGroupName: string | null;
  subjectId: string | null;
  subjectName: string | null;
  staffMemberId: string | null;
  staffName: string | null;
  message: string;
};

export type TimetableReadiness = {
  canGenerate: boolean;
  academicSessionId: string;
  academicSessionName: string;
  weeklyCapacity: number;
  totalClasses: number;
  readyClasses: number;
  requirementCount: number;
  coveredRequirementCount: number;
  issues: TimetableReadinessIssue[];
};

export type GeneratedTimetableEntry = {
  id: string;
  classGroupId: string;
  classGroupName: string;
  academicLevelName: string;
  subjectId: string;
  subjectName: string;
  staffMemberId: string;
  staffName: string;
  dayOfWeek: number | string;
  periodNumber: number;
  startTime: string;
  endTime: string;
  parallelSubjectGroupId?: string | null;
  parallelOccurrenceId?: string | null;
  parallelDisplayName?: string | null;
  parallelMembers?: { subjectId: string; staffMemberId: string; subjectName: string; staffName: string }[];
};

export type GeneratedTimetable = {
  id: string;
  academicSessionId: string;
  academicTermId: string;
  generatedAtUtc: string;
  versionNumber: number;
  isActive: boolean;
  entryCount: number;
  entries: GeneratedTimetableEntry[];
};
