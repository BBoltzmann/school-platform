"use client";

import {
  FormEvent,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  GraduationCap,
  LoaderCircle,
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
import { Input } from "@/components/ui/input";
import type {
  StudentEnrollment,
  StudentSetup,
} from "@/types/students";

type ChangeStudentPlacementDialogProps = {
  studentId: string;
  setup: StudentSetup;
  currentEnrollment:
    | StudentEnrollment
    | undefined;
};

export function ChangeStudentPlacementDialog({
  studentId,
  setup,
  currentEnrollment,
}: ChangeStudentPlacementDialogProps) {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [
    academicLevelId,
    setAcademicLevelId,
  ] = useState(
    currentEnrollment
      ?.academicLevelId ??
      setup.levels[0]?.id ??
      ""
  );

  const [
    classGroupId,
    setClassGroupId,
  ] = useState(
    currentEnrollment
      ?.classGroupId ?? ""
  );

  const [
    enrollmentDate,
    setEnrollmentDate,
  ] = useState(
    currentEnrollment
      ?.enrollmentDate ??
      setup.currentSession
        ?.startDate ??
      ""
  );

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const availableClasses =
    useMemo(
      () =>
        setup.classes.filter(
          (classGroup) =>
            classGroup.academicLevelId ===
            academicLevelId
        ),
      [
        setup.classes,
        academicLevelId,
      ]
    );

  useEffect(() => {
    if (!open) {
      return;
    }

    const levelId =
      currentEnrollment
        ?.academicLevelId ??
      setup.levels[0]?.id ??
      "";

    setAcademicLevelId(levelId);

    const date =
      currentEnrollment
        ?.enrollmentDate ??
      setup.currentSession
        ?.startDate ??
      "";

    const session =
      setup.currentSession;

    const dateIsValid =
      session &&
      date >= session.startDate &&
      date <= session.endDate;

    setEnrollmentDate(
      dateIsValid
        ? date
        : session?.startDate ?? ""
    );

    setError(null);
  }, [
    open,
    currentEnrollment,
    setup,
  ]);

  useEffect(() => {
    const valid =
      availableClasses.some(
        (classGroup) =>
          classGroup.id ===
          classGroupId
      );

    if (!valid) {
      setClassGroupId(
        availableClasses[0]?.id ?? ""
      );
    }
  }, [
    availableClasses,
    classGroupId,
  ]);

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setSaving(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/students/${studentId}/placement`,
        {
          method: "PATCH",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            academicLevelId,
            classGroupId,
            enrollmentDate,
          }),
        }
      );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to change placement."
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

  return (
    <Dialog
      open={open}
      onOpenChange={setOpen}
    >
      <DialogTrigger
        render={
          <Button variant="outline">
            <GraduationCap className="mr-2 h-4 w-4" />
            Change Placement
          </Button>
        }
      />

      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            Change Student Placement
          </DialogTitle>

          <DialogDescription>
            Update the student's level,
            class or enrolment date for the
            current academic session.
          </DialogDescription>
        </DialogHeader>

        {!setup.currentSession ? (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
            There is no current academic
            session.
          </div>
        ) : (
          <form
            onSubmit={handleSubmit}
            className="space-y-5 pt-2"
          >
            <div className="rounded-lg bg-muted/50 p-4">
              <div className="text-xs text-muted-foreground">
                Current Academic Session
              </div>

              <div className="mt-1 font-semibold">
                {
                  setup.currentSession.name
                }
              </div>

              <div className="mt-1 text-xs text-muted-foreground">
                {
                  setup.currentSession
                    .startDate
                }{" "}
                –{" "}
                {
                  setup.currentSession
                    .endDate
                }
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Academic Level
              </label>

              <select
                value={academicLevelId}
                onChange={(event) =>
                  setAcademicLevelId(
                    event.target.value
                  )
                }
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                required
              >
                {setup.levels.map(
                  (level) => (
                    <option
                      key={level.id}
                      value={level.id}
                    >
                      {level.name}
                    </option>
                  )
                )}
              </select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Class
              </label>

              <select
                value={classGroupId}
                onChange={(event) =>
                  setClassGroupId(
                    event.target.value
                  )
                }
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                required
              >
                {availableClasses.map(
                  (classGroup) => (
                    <option
                      key={classGroup.id}
                      value={classGroup.id}
                    >
                      {classGroup.name}
                    </option>
                  )
                )}
              </select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Enrolment Date
              </label>

              <Input
                type="date"
                min={
                  setup.currentSession
                    .startDate
                }
                max={
                  setup.currentSession
                    .endDate
                }
                value={enrollmentDate}
                onChange={(event) =>
                  setEnrollmentDate(
                    event.target.value
                  )
                }
                required
              />

              <p className="text-xs text-muted-foreground">
                Must fall inside the current
                academic session.
              </p>
            </div>

            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                {error}
              </div>
            )}

            <div className="flex justify-end gap-3 border-t pt-5">
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
                  !academicLevelId ||
                  !classGroupId
                }
                className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
              >
                {saving && (
                  <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                )}

                Update Placement
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
