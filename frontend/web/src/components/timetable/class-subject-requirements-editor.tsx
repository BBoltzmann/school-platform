"use client";

import {
  useEffect,
  useMemo,
  useState,
} from "react";
import {
  AlertTriangle,
  BookOpen,
  LoaderCircle,
  Save,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type {
  ClassSubjectRequirements,
  TimetableClassOption,
  TimetableSubjectOption,
} from "@/types/timetable";

export function ClassSubjectRequirementsEditor({
  classes,
  subjects,
  sessionName,
  weeklyCapacity,
}: {
  classes: TimetableClassOption[];
  subjects: TimetableSubjectOption[];
  sessionName: string;
  weeklyCapacity: number | null;
}) {
  const [
    classGroupId,
    setClassGroupId,
  ] = useState(
    classes[0]?.id ?? ""
  );

  const [
    periods,
    setPeriods,
  ] = useState<
    Record<string, number>
  >({});

  const [loading, setLoading] =
    useState(false);

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null
    );

  const [success, setSuccess] =
    useState<string | null>(
      null
    );

  const selectedClass =
    classes.find(
      (item) =>
        item.id === classGroupId
    );

  const totalAllocated =
    useMemo(
      () =>
        Object.values(
          periods
        ).reduce(
          (sum, value) =>
            sum +
            Math.max(
              0,
              Number(value) || 0
            ),
          0
        ),
      [periods]
    );

  const remaining =
    weeklyCapacity === null
      ? null
      : weeklyCapacity -
        totalAllocated;

  useEffect(() => {
    if (!classGroupId) {
      setPeriods({});
      return;
    }

    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      setSuccess(null);

      try {
        const response =
          await fetch(
            `/api/timetable/classes/${classGroupId}/requirements`
          );

        const result =
          (await response.json()) as
            | ClassSubjectRequirements
            | {
                error?: string;
              };

        if (!response.ok) {
          throw new Error(
            "error" in result
              ? result.error ??
                  "Unable to load class requirements."
              : "Unable to load class requirements."
          );
        }

        if (cancelled) {
          return;
        }

        const loaded:
          Record<string, number> =
            {};

        for (
          const subject
          of subjects
        ) {
          loaded[subject.id] = 0;
        }

        for (
          const requirement
          of (
            result as ClassSubjectRequirements
          ).requirements
        ) {
          loaded[
            requirement.subjectId
          ] =
            requirement.periodsPerWeek;
        }

        setPeriods(loaded);
      } catch (exception) {
        if (!cancelled) {
          setError(
            exception instanceof
            Error
              ? exception.message
              : "Unable to load class requirements."
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    load();

    return () => {
      cancelled = true;
    };
  }, [
    classGroupId,
    subjects,
  ]);

  function updatePeriods(
    subjectId: string,
    value: string
  ) {
    const number =
      Number(value);

    setPeriods(
      (current) => ({
        ...current,
        [subjectId]:
          Number.isFinite(number)
            ? Math.max(
                0,
                Math.min(
                  50,
                  number
                )
              )
            : 0,
      })
    );

    setSuccess(null);
  }

  async function save() {
    if (!classGroupId) {
      setError(
        "Select a class."
      );
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(null);

    const requirements =
      subjects
        .map((subject) => ({
          subjectId:
            subject.id,
          periodsPerWeek:
            periods[
              subject.id
            ] ?? 0,
        }))
        .filter(
          (item) =>
            item.periodsPerWeek >
            0
        );

    try {
      const response =
        await fetch(
          `/api/timetable/classes/${classGroupId}/requirements`,
          {
            method: "PUT",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify({
              requirements,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to save weekly period requirements."
        );

        return;
      }

      setSuccess(
        "Weekly subject requirements saved."
      );
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setSaving(false);
    }
  }

  if (classes.length === 0) {
    return (
      <section className="rounded-xl border bg-card p-8 text-center shadow-sm">
        <BookOpen className="mx-auto h-9 w-9 text-muted-foreground" />

        <h2 className="mt-3 font-semibold">
          No classes configured
        </h2>

        <p className="mt-1 text-sm text-muted-foreground">
          Create classes in Academic
          Setup before configuring
          weekly subject periods.
        </p>
      </section>
    );
  }

  return (
    <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className="border-b px-5 py-4">
        <div className="flex items-start gap-3">
          <BookOpen className="mt-0.5 h-5 w-5" />

          <div>
            <h2 className="font-semibold">
              Class Subject Requirements
            </h2>

            <p className="mt-1 text-xs text-muted-foreground">
              Set how many periods each
              subject requires every week
              for {sessionName}.
            </p>
          </div>
        </div>
      </div>

      <div className="space-y-6 p-5">
        <div className="grid gap-4 lg:grid-cols-[1fr_1fr]">
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
            >
              {classes.map(
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
              )}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-lg border p-3">
              <div className="text-xs text-muted-foreground">
                Allocated
              </div>

              <div className="mt-1 text-xl font-bold">
                {totalAllocated}
              </div>

              <div className="text-xs text-muted-foreground">
                periods/week
              </div>
            </div>

            <div className="rounded-lg border p-3">
              <div className="text-xs text-muted-foreground">
                Remaining
              </div>

              <div
                className={
                  remaining !== null &&
                  remaining < 0
                    ? "mt-1 text-xl font-bold text-red-600"
                    : "mt-1 text-xl font-bold"
                }
              >
                {remaining === null
                  ? "—"
                  : remaining}
              </div>

              <div className="text-xs text-muted-foreground">
                of{" "}
                {weeklyCapacity ??
                  "—"}{" "}
                available
              </div>
            </div>
          </div>
        </div>

        {remaining !== null &&
          remaining < 0 && (
            <div className="flex gap-3 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
              <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />

              <div>
                This class currently
                requires{" "}
                {Math.abs(
                  remaining
                )}{" "}
                more periods than the
                configured timetable can
                contain. Timetable
                readiness will fail until
                this is corrected.
              </div>
            </div>
          )}

        {loading ? (
          <div className="flex items-center justify-center py-12">
            <LoaderCircle className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <div className="overflow-hidden rounded-lg border">
            <div className="grid grid-cols-[1fr_150px] border-b bg-muted/40 px-4 py-3 text-xs font-medium text-muted-foreground">
              <div>
                Subject
              </div>

              <div>
                Periods / Week
              </div>
            </div>

            <div className="divide-y">
              {subjects.map(
                (subject) => (
                  <div
                    key={
                      subject.id
                    }
                    className="grid grid-cols-[1fr_150px] items-center gap-4 px-4 py-3"
                  >
                    <div className="text-sm font-medium">
                      {
                        subject.name
                      }
                    </div>

                    <input
                      type="number"
                      min={0}
                      max={50}
                      value={
                        periods[
                          subject.id
                        ] ?? 0
                      }
                      onChange={(
                        event
                      ) =>
                        updatePeriods(
                          subject.id,
                          event.target
                            .value
                        )
                      }
                      className="h-9 w-full rounded-md border bg-background px-3 text-sm"
                    />
                  </div>
                )
              )}
            </div>
          </div>
        )}

        <p className="text-xs text-muted-foreground">
          Set a subject to 0 if it is not
          taught to{" "}
          {selectedClass?.name ??
            "this class"}.
        </p>

        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {error}
          </div>
        )}

        {success && (
          <div className="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-700">
            {success}
          </div>
        )}

        <div className="flex justify-end border-t pt-5">
          <Button
            type="button"
            disabled={
              saving ||
              loading
            }
            onClick={save}
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            {saving ? (
              <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Save className="mr-2 h-4 w-4" />
            )}

            Save Weekly Periods
          </Button>
        </div>
      </div>
    </section>
  );
}
