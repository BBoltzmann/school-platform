import {
  GraduationCap,
  UserRound,
  UsersRound,
} from "lucide-react";

import { CreateStudentDialog } from "@/components/students/create-student-dialog";
import { StudentsDirectory } from "@/components/students/students-directory";
import {
  getStudents,
  getStudentSetup,
} from "@/lib/api/students";

type StudentsPageProps = {
  params: Promise<{
    tenantSlug: string;
  }>;
};

export default async function StudentsPage({
  params,
}: StudentsPageProps) {
  const { tenantSlug } = await params;
  const [students, setup] =
    await Promise.all([
      getStudents(),
      getStudentSetup(),
    ]);

  const activeStudents =
    students.filter(
      (student) =>
        student.isActive
    ).length;

  const enrolledStudents =
    students.filter(
      (student) =>
        student.currentEnrollment !==
        null
    ).length;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">
            Students
          </h1>

          <p className="mt-1 text-sm text-muted-foreground">
            Manage student records,
            academic placement and
            enrolment.
          </p>
        </div>

        <CreateStudentDialog
          setup={setup}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border bg-card p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">
                Total Students
              </p>

              <p className="mt-2 text-3xl font-bold">
                {students.length}
              </p>
            </div>

            <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-muted">
              <UsersRound className="h-5 w-5" />
            </div>
          </div>
        </div>

        <div className="rounded-xl border bg-card p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">
                Active Students
              </p>

              <p className="mt-2 text-3xl font-bold">
                {activeStudents}
              </p>
            </div>

            <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-green-50 text-green-700">
              <UserRound className="h-5 w-5" />
            </div>
          </div>
        </div>

        <div className="rounded-xl border bg-card p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">
                Current Enrolments
              </p>

              <p className="mt-2 text-3xl font-bold">
                {enrolledStudents}
              </p>
            </div>

            <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-amber-50">
              <GraduationCap className="h-5 w-5" />
            </div>
          </div>
        </div>
      </div>

      <StudentsDirectory students={students} setup={setup} tenantSlug={tenantSlug} />
    </div>
  );
}
