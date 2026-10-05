import type { ReactNode } from "react";
import {
  BookOpen,
  Building2,
  CalendarDays,
  School,
  UsersRound,
} from "lucide-react";

import { CreateClassDialog } from "@/components/academics/create-class-dialog";
import { ClassActions } from "@/components/academics/class-actions";
import { ClassSubjectActions } from "@/components/academics/class-subject-actions";
import { SubjectActions } from "@/components/academics/subject-actions";
import { CreateLevelDialog } from "@/components/academics/create-level-dialog";
import { LevelActions } from "@/components/academics/level-actions";
import { CreateSessionDialog } from "@/components/academics/create-session-dialog";
import { CreateSubjectDialog } from "@/components/academics/create-subject-dialog";
import { CreateTermDialog } from "@/components/academics/create-term-dialog";
import { getAcademicSetup } from "@/lib/api/academics";

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(new Date(`${value}T00:00:00`));
}

function SectionTitle({
  icon,
  title,
  description,
}: {
  icon: ReactNode;
  title: string;
  description: string;
}) {
  return (
    <div className="flex items-start gap-3">
      <div className="mt-0.5 h-8 w-1 rounded-full bg-tenant-primary" />

      <div className="mt-0.5 text-muted-foreground">
        {icon}
      </div>

      <div>
        <h2 className="text-base font-semibold">
          {title}
        </h2>

        <p className="mt-0.5 text-sm text-muted-foreground">
          {description}
        </p>
      </div>
    </div>
  );
}

function StatusBadge({
  active,
}: {
  active: boolean;
}) {
  return (
    <span
      className={
        active
          ? "inline-flex rounded-full bg-green-50 px-2.5 py-1 text-xs font-medium text-green-700"
          : "inline-flex rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-600"
      }
    >
      {active ? "Active" : "Inactive"}
    </span>
  );
}

export default async function AcademicsPage() {
  const setup = await getAcademicSetup();

  const currentSession = setup.currentSession;

  function getCampusName(campusId: string) {
    return (
      setup.campuses.find(
        (campus) => campus.id === campusId
      )?.name ?? "Unknown Campus"
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          Academic Setup
        </h1>

        <p className="mt-1 text-sm text-muted-foreground">
          Configure academic sessions, terms,
          levels, classes and subjects for the
          school.
        </p>
      </div>

      {/* Academic Sessions */}
      <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="flex flex-col gap-4 border-b px-5 py-5 sm:flex-row sm:items-center sm:justify-between">
          <SectionTitle
            icon={
              <CalendarDays className="h-5 w-5" />
            }
            title="Academic Sessions"
            description="Manage school academic sessions."
          />

          <CreateSessionDialog />
        </div>

        <div className="p-5">
          {currentSession ? (
            <div className="grid gap-6 xl:grid-cols-[280px_1fr]">
              <div className="flex gap-4 xl:border-r xl:pr-6">
                <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-amber-50">
                  <CalendarDays className="h-5 w-5" />
                </div>

                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="text-xl font-bold">
                      {currentSession.name}
                    </h3>

                    {currentSession.isCurrent && (
                      <span className="rounded-full bg-green-50 px-2.5 py-1 text-xs font-medium text-green-700">
                        Current
                      </span>
                    )}
                  </div>

                  <div className="mt-3 text-sm text-muted-foreground">
                    {formatDate(
                      currentSession.startDate
                    )}{" "}
                    –{" "}
                    {formatDate(
                      currentSession.endDate
                    )}
                  </div>
                </div>
              </div>

              <div>
                <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <h3 className="font-semibold">
                    Terms in this Session
                  </h3>

                  <CreateTermDialog
                    academicSessionId={
                      currentSession.id
                    }
                  />
                </div>

                {currentSession.terms.length >
                0 ? (
                  <div className="grid gap-3 md:grid-cols-2 2xl:grid-cols-3">
                    {currentSession.terms.map(
                      (term) => (
                        <div
                          key={term.id}
                          className="rounded-lg border bg-background p-4"
                        >
                          <div className="flex items-center gap-2">
                            <span className="h-2 w-2 rounded-full bg-green-500" />

                            <span className="font-semibold">
                              {term.name}
                            </span>
                          </div>

                          <p className="mt-2 text-sm text-muted-foreground">
                            {formatDate(
                              term.startDate
                            )}{" "}
                            –{" "}
                            {formatDate(
                              term.endDate
                            )}
                          </p>
                        </div>
                      )
                    )}
                  </div>
                ) : (
                  <div className="rounded-lg border border-dashed p-8 text-center">
                    <p className="text-sm text-muted-foreground">
                      No terms have been created
                      for this session.
                    </p>
                  </div>
                )}
              </div>
            </div>
          ) : (
            <div className="rounded-lg border border-dashed py-12 text-center">
              <CalendarDays className="mx-auto h-9 w-9 text-muted-foreground" />

              <h3 className="mt-4 font-semibold">
                No academic session
              </h3>

              <p className="mt-1 text-sm text-muted-foreground">
                Create an academic session to
                begin configuring the school.
              </p>

              <div className="mt-5 flex justify-center">
                <CreateSessionDialog />
              </div>
            </div>
          )}
        </div>
      </section>

      {/* Levels and Classes */}
      <div className="grid gap-6 xl:grid-cols-2">
        {/* Levels */}
        <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
          <div className="flex items-center justify-between gap-4 border-b px-5 py-5">
            <SectionTitle
              icon={
                <School className="h-5 w-5" />
              }
              title="Levels"
              description="Manage academic levels in the school."
            />

            <CreateLevelDialog />
          </div>

          {setup.levels.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="border-b bg-muted/40 text-xs text-muted-foreground">
                  <tr>
                    <th className="px-5 py-3 font-medium">
                      Level Name
                    </th>

                    <th className="px-3 py-3 font-medium">
                      Category
                    </th>

                    <th className="px-3 py-3 font-medium">
                      Classes
                    </th>

                    <th className="px-3 py-3 font-medium">
                      Status
                    </th>

                    <th className="w-10 px-3 py-3" />
                  </tr>
                </thead>

                <tbody>
                  {setup.levels.map(
                    (level) => (
                      <tr
                        key={level.id}
                        className="border-b last:border-b-0"
                      >
                        <td className="px-5 py-4 font-medium">
                          {level.name}
                        </td>

                        <td className="px-3 py-4 text-muted-foreground">
                          {level.category}
                        </td>

                        <td className="px-3 py-4">
                          {level.classCount}
                        </td>

                        <td className="px-3 py-4">
                          <StatusBadge
                            active={
                              level.isActive
                            }
                          />
                        </td>

                        <td className="px-3 py-4">
                          <LevelActions level={level} />
                        </td>
                      </tr>
                    )
                  )}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="px-5 py-12 text-center">
              <School className="mx-auto h-8 w-8 text-muted-foreground" />

              <h3 className="mt-3 font-medium">
                No levels yet
              </h3>

              <p className="mt-1 text-sm text-muted-foreground">
                Add the first academic level to
                begin building the school
                structure.
              </p>
            </div>
          )}
        </section>

        {/* Classes */}
        <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
          <div className="flex items-center justify-between gap-4 border-b px-5 py-5">
            <SectionTitle
              icon={
                <UsersRound className="h-5 w-5" />
              }
              title="Classes"
              description="Manage classes for each level."
            />

            <CreateClassDialog
              campuses={setup.campuses}
              levels={setup.levels}
            />
          </div>

          {setup.classes.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="border-b bg-muted/40 text-xs text-muted-foreground">
                  <tr>
                    <th className="px-5 py-3 font-medium">
                      Class
                    </th>

                    <th className="px-3 py-3 font-medium">
                      Level
                    </th>

                    <th className="px-3 py-3 font-medium">
                      Campus
                    </th>

                    <th className="px-3 py-3 font-medium">
                      Status
                    </th>

                    <th className="w-10 px-3 py-3" />
                  </tr>
                </thead>

                <tbody>
                  {setup.classes.map(
                    (classGroup) => (
                      <tr
                        key={classGroup.id}
                        className="border-b last:border-b-0"
                      >
                        <td className="px-5 py-4 font-semibold">
                          {classGroup.name}
                        </td>

                        <td className="px-3 py-4 text-muted-foreground">
                          {
                            classGroup.academicLevelName
                          }
                        </td>

                        <td className="px-3 py-4 text-muted-foreground">
                          {getCampusName(
                            classGroup.campusId
                          )}
                        </td>

                        <td className="px-3 py-4">
                          <StatusBadge
                            active={
                              classGroup.isActive
                            }
                          />
                        </td>

                        <td className="px-3 py-4">
                          <ClassActions
                            classGroup={classGroup}
                            campuses={setup.campuses}
                            levels={setup.levels}
                          />
                          <ClassSubjectActions classGroup={classGroup} subjects={setup.subjects} academicSessionId={currentSession?.id ?? ""} />
                        </td>
                      </tr>
                    )
                  )}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="px-5 py-12 text-center">
              <UsersRound className="mx-auto h-8 w-8 text-muted-foreground" />

              <h3 className="mt-3 font-medium">
                No classes yet
              </h3>

              <p className="mt-1 text-sm text-muted-foreground">
                {setup.levels.length === 0
                  ? "Create an academic level before adding classes."
                  : setup.campuses.length === 0
                    ? "An active campus is required before adding classes."
                    : "Add the first class to an academic level."}
              </p>
            </div>
          )}
        </section>
      </div>

      {/* Campuses */}
      <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="border-b px-5 py-5">
          <SectionTitle
            icon={
              <Building2 className="h-5 w-5" />
            }
            title="Campuses"
            description="Active campuses available for class assignment."
          />
        </div>

        {setup.campuses.length > 0 ? (
          <div className="grid gap-3 p-5 sm:grid-cols-2 lg:grid-cols-3">
            {setup.campuses.map(
              (campus) => (
                <div
                  key={campus.id}
                  className="flex items-center justify-between rounded-lg border p-4"
                >
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
                      <Building2 className="h-5 w-5" />
                    </div>

                    <div>
                      <div className="font-medium">
                        {campus.name}
                      </div>

                      <div className="text-xs text-muted-foreground">
                        School campus
                      </div>
                    </div>
                  </div>

                  <StatusBadge
                    active={campus.isActive}
                  />
                </div>
              )
            )}
          </div>
        ) : (
          <div className="px-5 py-10 text-center text-sm text-muted-foreground">
            No active campuses found.
          </div>
        )}
      </section>

      {/* Subjects */}
      <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="flex items-center justify-between gap-4 border-b px-5 py-5">
          <SectionTitle
            icon={
              <BookOpen className="h-5 w-5" />
            }
            title="Subjects"
            description="Manage subjects offered by the school."
          />

          <CreateSubjectDialog />
        </div>

        {setup.subjects.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="border-b bg-muted/40 text-xs text-muted-foreground">
                <tr>
                  <th className="px-5 py-3 font-medium">
                    Subject Name
                  </th>

                  <th className="px-3 py-3 font-medium">
                    Code
                  </th>

                  <th className="px-3 py-3 font-medium">
                    Category
                  </th>

                  <th className="px-3 py-3 font-medium">
                    Status
                  </th>

                  <th className="w-10 px-3 py-3" />
                </tr>
              </thead>

              <tbody>
                {setup.subjects.map(
                  (subject) => (
                    <tr
                      key={subject.id}
                      className="border-b last:border-b-0"
                    >
                      <td className="px-5 py-4 font-medium">
                        {subject.name}
                      </td>

                      <td className="px-3 py-4 font-mono text-xs font-medium">
                        {subject.code}
                      </td>

                      <td className="px-3 py-4 text-muted-foreground">
                        {subject.category}
                      </td>

                      <td className="px-3 py-4">
                        <StatusBadge
                          active={
                            subject.isActive
                          }
                        />
                      </td>

                      <td className="px-3 py-4">
                        <SubjectActions subject={subject} />
                      </td>
                    </tr>
                  )
                )}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="px-5 py-12 text-center">
            <BookOpen className="mx-auto h-8 w-8 text-muted-foreground" />

            <h3 className="mt-3 font-medium">
              No subjects yet
            </h3>

            <p className="mt-1 text-sm text-muted-foreground">
              Add the first subject offered by
              the school.
            </p>
          </div>
        )}
      </section>
    </div>
  );
}
