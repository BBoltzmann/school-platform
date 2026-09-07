"use client";

import {
  FormEvent,
  useMemo,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  BookOpen,
  GraduationCap,
  LoaderCircle,
  Plus,
  Trash2,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";

import type {
  TeachingAssignment,
  TeachingAssignmentSetup,
} from "@/types/teaching-assignments";

type TeachingAssignmentsCardProps = {
  staffId: string;
  assignments: TeachingAssignment[];
  setup: TeachingAssignmentSetup;
};

export function TeachingAssignmentsCard({
  staffId,
  assignments,
  setup,
}: TeachingAssignmentsCardProps) {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [classGroupId, setClassGroupId] =
    useState(
      setup.classes[0]?.id ?? ""
    );

  const [subjectId, setSubjectId] =
    useState(
      setup.subjects[0]?.id ?? ""
    );

  const [saving, setSaving] =
    useState(false);

  const [
    deletingId,
    setDeletingId,
  ] = useState<string | null>(
    null
  );

  const [error, setError] =
    useState<string | null>(
      null
    );

  const groupedAssignments =
    useMemo(() => {
      const groups =
        new Map<
          string,
          {
            sessionName: string;
            assignments: TeachingAssignment[];
          }
        >();

      for (const assignment of assignments) {
        const existing =
          groups.get(
            assignment.academicSessionId
          );

        if (existing) {
          existing.assignments.push(
            assignment
          );
        } else {
          groups.set(
            assignment.academicSessionId,
            {
              sessionName:
                assignment.academicSessionName,
              assignments: [
                assignment,
              ],
            }
          );
        }
      }

      return Array.from(
        groups.values()
      );
    }, [assignments]);

  async function addAssignment(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (!setup.currentSession) {
      setError(
        "A current academic session is required."
      );

      return;
    }

    if (!classGroupId) {
      setError(
        "Select a class."
      );

      return;
    }

    if (!subjectId) {
      setError(
        "Select a subject."
      );

      return;
    }

    setSaving(true);
    setError(null);

    try {
      const response =
        await fetch(
          "/api/teaching-assignments",
          {
            method: "POST",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify({
              staffMemberId:
                staffId,
              academicSessionId:
                setup.currentSession.id,
              classGroupId,
              subjectId,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to create teaching assignment."
        );

        return;
      }

      setOpen(false);
      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setSaving(false);
    }
  }

  async function removeAssignment(
    assignment: TeachingAssignment
  ) {
    const confirmed =
      window.confirm(
        `Remove ${assignment.subjectName} from ${assignment.classGroupName}?`
      );

    if (!confirmed) {
      return;
    }

    setDeletingId(
      assignment.id
    );

    setError(null);

    try {
      const response =
        await fetch(
          `/api/teaching-assignments/${assignment.id}`,
          {
            method: "DELETE",
          }
        );

      if (!response.ok) {
        let message =
          "Unable to remove teaching assignment.";

        try {
          const result =
            await response.json();

          message =
            result.error ??
            message;
        } catch {
          // Keep default.
        }

        setError(message);

        return;
      }

      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className="flex flex-col gap-4 border-b px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-start gap-2">
          <GraduationCap className="mt-0.5 h-5 w-5" />

          <div>
            <h2 className="font-semibold">
              Teaching Assignments
            </h2>

            <p className="mt-0.5 text-xs text-muted-foreground">
              Classes and subjects assigned
              to this teacher.
            </p>
          </div>
        </div>

        <Dialog
          open={open}
          onOpenChange={(value) => {
            setOpen(value);

            if (!value) {
              setError(null);
            }
          }}
        >
          <DialogTrigger
            render={
              <Button
                size="sm"
                className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
              >
                <Plus className="mr-2 h-4 w-4" />
                Add Assignment
              </Button>
            }
          />

          <DialogContent className="sm:max-w-lg">
            <DialogHeader>
              <DialogTitle>
                Add Teaching Assignment
              </DialogTitle>

              <DialogDescription>
                Assign a class and subject
                to this teacher for the
                current academic session.
              </DialogDescription>
            </DialogHeader>

            {!setup.currentSession ? (
              <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
                Set a current academic
                session before creating
                teaching assignments.
              </div>
            ) : (
              <form
                onSubmit={
                  addAssignment
                }
                className="space-y-5"
              >
                <div className="space-y-2">
                  <label className="text-sm font-medium">
                    Academic Session
                  </label>

                  <div className="flex h-10 items-center rounded-md border bg-muted/40 px-3 text-sm">
                    {
                      setup.currentSession.name
                    }
                  </div>
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium">
                    Class
                  </label>

                  <select
                    value={
                      classGroupId
                    }
                    onChange={(event) =>
                      setClassGroupId(
                        event.target.value
                      )
                    }
                    className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                    required
                  >
                    {setup.classes.length ===
                    0 ? (
                      <option value="">
                        No classes available
                      </option>
                    ) : (
                      setup.classes.map(
                        (item) => (
                          <option
                            key={item.id}
                            value={item.id}
                          >
                            {
                              item.academicLevelName
                            }
                            {" — "}
                            {item.name}
                          </option>
                        )
                      )
                    )}
                  </select>
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium">
                    Subject
                  </label>

                  <select
                    value={
                      subjectId
                    }
                    onChange={(event) =>
                      setSubjectId(
                        event.target.value
                      )
                    }
                    className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                    required
                  >
                    {setup.subjects.length ===
                    0 ? (
                      <option value="">
                        No subjects available
                      </option>
                    ) : (
                      setup.subjects.map(
                        (subject) => (
                          <option
                            key={
                              subject.id
                            }
                            value={
                              subject.id
                            }
                          >
                            {
                              subject.name
                            }
                          </option>
                        )
                      )
                    )}
                  </select>
                </div>

                {error && (
                  <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                    {error}
                  </div>
                )}

                <div className="flex justify-end gap-3 border-t pt-4">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() =>
                      setOpen(false)
                    }
                  >
                    Cancel
                  </Button>

                  <Button
                    type="submit"
                    disabled={
                      saving ||
                      !classGroupId ||
                      !subjectId
                    }
                    className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
                  >
                    {saving && (
                      <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                    )}

                    Add Assignment
                  </Button>
                </div>
              </form>
            )}
          </DialogContent>
        </Dialog>
      </div>

      {error && !open && (
        <div className="border-b border-red-200 bg-red-50 px-5 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {assignments.length === 0 ? (
        <div className="px-5 py-12 text-center">
          <BookOpen className="mx-auto h-9 w-9 text-muted-foreground" />

          <h3 className="mt-3 font-medium">
            No teaching assignments yet
          </h3>

          <p className="mx-auto mt-1 max-w-md text-sm text-muted-foreground">
            Add the classes and subjects
            this teacher is responsible
            for.
          </p>
        </div>
      ) : (
        <div className="divide-y">
          {groupedAssignments.map(
            (group) => (
              <div
                key={group.sessionName}
                className="p-5"
              >
                <div className="mb-4 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  {group.sessionName}
                </div>

                <div className="overflow-hidden rounded-lg border">
                  <table className="w-full text-sm">
                    <thead className="border-b bg-muted/40 text-left">
                      <tr>
                        <th className="px-4 py-3 font-medium">
                          Level
                        </th>

                        <th className="px-4 py-3 font-medium">
                          Class
                        </th>

                        <th className="px-4 py-3 font-medium">
                          Subject
                        </th>

                        <th className="w-20 px-4 py-3 font-medium">
                          Action
                        </th>
                      </tr>
                    </thead>

                    <tbody className="divide-y">
                      {group.assignments.map(
                        (
                          assignment
                        ) => (
                          <tr
                            key={
                              assignment.id
                            }
                          >
                            <td className="px-4 py-3 text-muted-foreground">
                              {
                                assignment.academicLevelName
                              }
                            </td>

                            <td className="px-4 py-3 font-medium">
                              {
                                assignment.classGroupName
                              }
                            </td>

                            <td className="px-4 py-3">
                              {
                                assignment.subjectName
                              }
                            </td>

                            <td className="px-4 py-3">
                              <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                disabled={
                                  deletingId ===
                                  assignment.id
                                }
                                onClick={() =>
                                  removeAssignment(
                                    assignment
                                  )
                                }
                              >
                                {deletingId ===
                                assignment.id ? (
                                  <LoaderCircle className="h-4 w-4 animate-spin" />
                                ) : (
                                  <Trash2 className="h-4 w-4" />
                                )}

                                <span className="sr-only">
                                  Remove
                                </span>
                              </Button>
                            </td>
                          </tr>
                        )
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            )
          )}
        </div>
      )}
    </section>
  );
}
