export type AssessmentSession = {
  id: string;
  name: string;
};

export type AssessmentTerm = {
  id: string;
  name: string;
};

export type AssessmentClass = {
  id: string;
  name: string;
  academicLevelId: string;
  academicLevelName: string;
};

export type AssessmentSubject = {
  id: string;
  name: string;
};

export type AssessmentSetup = {
  currentSession: AssessmentSession | null;
  terms: AssessmentTerm[];
  classes: AssessmentClass[];
  subjects: AssessmentSubject[];
};

export type Assessment = {
  id: string;
  academicSessionId: string;
  academicTermId: string;
  classGroupId: string;
  classGroupName: string;
  academicLevelName: string;
  subjectId: string;
  subjectName: string;
  type: string;
  title: string;
  maximumScore: number;
  weightPercentage: number;
  sortOrder: number;
  isActive: boolean;
};

export type AssessmentStudentScore = {
  studentId: string;
  admissionNumber: string;
  studentName: string;
  rawScore: number | null;
  percentageScore: number | null;
  weightedContribution: number | null;
};

export type AssessmentScoreSheet = {
  assessment: Assessment;
  configuredWeightTotal: number;
  students: AssessmentStudentScore[];
};

export type GradebookAssessment = {
  assessmentId: string;
  type: string;
  title: string;
  maximumScore: number;
  weightPercentage: number;
};

export type GradebookScore = {
  assessmentId: string;
  rawScore: number | null;
  percentageScore: number | null;
  weightedContribution: number | null;
};

export type GradebookStudent = {
  studentId: string;
  admissionNumber: string;
  studentName: string;
  totalWeightedScore: number;
  scores: GradebookScore[];
};

export type AssessmentGradebook = {
  academicTermId: string;
  classGroupId: string;
  classGroupName: string;
  subjectId: string;
  subjectName: string;
  configuredWeightTotal: number;
  isComplete: boolean;
  assessments: GradebookAssessment[];
  students: GradebookStudent[];
};
