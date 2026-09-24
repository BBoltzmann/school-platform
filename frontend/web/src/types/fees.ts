export type FeesOption = {
  id: string;
  name: string;
};

export type FeesStudentOption = {
  id: string;
  name: string;
  admissionNumber: string;
  className: string | null;
};

export type FeeStructureStudent = {
  studentId: string;
  studentName: string;
  admissionNumber: string;
  className: string | null;
  assignedAtUtc: string;
};

export type FeeItem = {
  id: string;
  name: string;
  code: string | null;
  description: string | null;
};

export type FeeStructureLine = {
  id: string;
  feeItemId: string;
  feeItemName: string;
  amount: number;
  isRequired: boolean;
};

export type FeeStructure = {
  id: string;
  academicSessionId: string;
  academicTermId: string;
  name: string;
  audienceType: string;
  audienceId: string | null;
  audienceName: string;
  totalRequiredAmount: number;
  lines: FeeStructureLine[];
};

export type FeesSetup = {
  currentSession: {
    id: string;
    name: string;
  } | null;
  terms: FeesOption[];
  levels: FeesOption[];
  classes: FeesOption[];
  students: FeesStudentOption[];
  feeItems: FeeItem[];
  structures: FeeStructure[];
};

export type OutstandingStudent = {
  studentId: string;
  admissionNumber: string;
  studentName: string;
  totalCharges: number;
  totalPaid: number;
  outstandingBalance: number;
};

export type RecentFeePayment = {
  paymentId: string;
  studentId: string;
  admissionNumber: string;
  studentName: string;
  amount: number;
  paymentMethod: string;
  receiptNumber: string;
  reference: string | null;
  createdAtUtc: string;
};

export type FeesOverview = {
  academicTermId: string;
  totalBilled: number;
  totalCollected: number;
  totalOutstanding: number;
  creditBalance: number;
  collectionRate: number;
  studentAccountCount: number;
  studentsOwing: number;
  fullyPaidStudents: number;
  topOutstanding: OutstandingStudent[];
  recentPayments: RecentFeePayment[];
};
