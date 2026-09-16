"use client";

import {
  useEffect,
  useMemo,
  useState,
} from "react";
import {
  AlertTriangle,
  CalendarCheck2,
  CheckCircle2,
  LoaderCircle,
  RefreshCw,
  Sparkles,
  UserRound,
  UsersRound,
  XCircle,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type {
  GeneratedTimetable,
  GeneratedTimetableEntry,
  TimetableReadiness,
  TimetableSettings,
  TimetableTermOption,
} from "@/types/timetable";

type ViewMode =
  | "class"
  | "teacher";

export function TimetableMvpPanel({
  readiness,
  terms,
  settings,
}: {
  readiness: TimetableReadiness;
  terms: TimetableTermOption[];
  settings: TimetableSettings | null;
}) {
  const [
    selectedTermId,
    setSelectedTermId,
  ] = useState(
    terms[0]?.id ?? ""
  );

  const [
    timetable,
    setTimetable,
  ] = useState<
    GeneratedTimetable | null
  >(null);

  const [
    loadingExisting,
    setLoadingExisting,
  ] = useState(false);

  const [
    generating,
    setGenerating,
  ] = useState(false);
  const [resetting, setResetting] = useState(false);

  const [
    generationError,
    setGenerationError,
  ] = useState<
    string | null
  >(null);

  const [viewMode, setViewMode] =
    useState<ViewMode>("class");

  const [
    selectedClassId,
    setSelectedClassId,
  ] = useState("");

  const [
    selectedTeacherId,
    setSelectedTeacherId,
  ] = useState("");

  useEffect(() => {
    if (!selectedTermId) {
      setTimetable(null);
      return;
    }

    let cancelled = false;

    async function load() {
      setLoadingExisting(true);
      setGenerationError(null);

      try {
        const response =
          await fetch(
            `/api/timetable/generated/${selectedTermId}`
          );

        if (
          response.status === 404
        ) {
          if (!cancelled) {
            setTimetable(null);
          }

          return;
        }

        const result =
          await response.json();

        if (!response.ok) {
          throw new Error(
            result.error ??
              "Unable to load generated timetable."
          );
        }

        if (!cancelled) {
          setTimetable(result);
        }
      } catch (exception) {
        if (!cancelled) {
          setGenerationError(
            exception instanceof Error
              ? exception.message
              : "Unable to load generated timetable."
          );
        }
      } finally {
        if (!cancelled) {
          setLoadingExisting(false);
        }
      }
    }

    load();

    return () => {
      cancelled = true;
    };
  }, [selectedTermId]);

  const classes =
    useMemo(() => {
      if (!timetable) {
        return [];
      }

      const map =
        new Map<
          string,
          string
        >();

      for (
        const entry
        of timetable.entries
      ) {
        map.set(
          entry.classGroupId,
          `${entry.academicLevelName} — ${entry.classGroupName}`
        );
      }

      return Array.from(
        map.entries()
      )
        .map(([id, name]) => ({
          id,
          name,
        }))
        .sort((a, b) =>
          a.name.localeCompare(
            b.name
          )
        );
    }, [timetable]);

  const teachers =
    useMemo(() => {
      if (!timetable) {
        return [];
      }

      const map =
        new Map<
          string,
          string
        >();

      for (
        const entry
        of timetable.entries
      ) {
        map.set(
          entry.staffMemberId,
          entry.staffName
        );
      }

      return Array.from(
        map.entries()
      )
        .map(([id, name]) => ({
          id,
          name,
        }))
        .sort((a, b) =>
          a.name.localeCompare(
            b.name
          )
        );
    }, [timetable]);

  useEffect(() => {
    setSelectedClassId(
      classes[0]?.id ?? ""
    );
  }, [classes]);

  useEffect(() => {
    setSelectedTeacherId(
      teachers[0]?.id ?? ""
    );
  }, [teachers]);

  async function generate() {
    if (!selectedTermId) {
      setGenerationError(
        "Select an academic term."
      );

      return;
    }

    setGenerating(true);
    setGenerationError(null);

    try {
      const response =
        await fetch(
          "/api/timetable/generate",
          {
            method: "POST",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify({
              academicTermId:
                selectedTermId,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        setGenerationError(
          result.error ??
            "Unable to generate timetable."
        );

        return;
      }

      setTimetable(result);
    } catch {
      setGenerationError(
        "Unable to connect to the server."
      );
    } finally {
      setGenerating(false);
    }
  }

  async function resetGenerated() {
    if (!selectedTermId || !window.confirm("Reset Generated Timetable?\n\nThis removes the generated lesson schedule for the selected term. It does not remove classes, subjects, teacher assignments, availability, or timetable structure. You can regenerate afterward.")) return;
    setResetting(true);
    setGenerationError(null);
    try {
      const response = await fetch(`/api/timetable/generated/${selectedTermId}/reset`, { method: "POST" });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.error ?? "Unable to reset generated timetable.");
      setTimetable(null);
    } catch (exception) {
      setGenerationError(exception instanceof Error ? exception.message : "Unable to reset generated timetable.");
    } finally { setResetting(false); }
  }

  return (
    <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className="border-b px-5 py-4">
        <div className="flex items-start gap-3">
          <CalendarCheck2 className="mt-0.5 h-5 w-5" />

          <div>
            <h2 className="font-semibold">
              Timetable Readiness &
              Generation
            </h2>

            <p className="mt-1 text-xs text-muted-foreground">
              Validate the current
              timetable data, then
              generate a timetable for
              the selected academic term.
            </p>
          </div>
        </div>
      </div>

      <div className="space-y-6 p-5">
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <ReadinessMetric
            label="Classes Ready"
            value={`${readiness.readyClasses}/${readiness.totalClasses}`}
            good={
              readiness.readyClasses ===
                readiness.totalClasses &&
              readiness.totalClasses >
                0
            }
          />

          <ReadinessMetric
            label="Teacher Coverage"
            value={`${readiness.coveredRequirementCount}/${readiness.requirementCount}`}
            good={
              readiness.coveredRequirementCount ===
                readiness.requirementCount &&
              readiness.requirementCount >
                0
            }
          />

          <ReadinessMetric
            label="Weekly Capacity"
            value={`${readiness.weeklyCapacity}`}
            good={
              readiness.weeklyCapacity >
              0
            }
          />

          <ReadinessMetric
            label="Overall"
            value={
              readiness.canGenerate
                ? "Ready"
                : "Not Ready"
            }
            good={
              readiness.canGenerate
            }
          />
        </div>

        {readiness.issues.length >
        0 ? (
          <div className="rounded-lg border border-red-200 bg-red-50">
            <div className="flex items-center gap-2 border-b border-red-200 px-4 py-3 font-medium text-red-800">
              <AlertTriangle className="h-4 w-4" />
              Resolve before generation
            </div>

            <div className="divide-y divide-red-100">
              {readiness.issues.map(
                (issue, index) => (
                  <div
                    key={`${issue.code}-${index}`}
                    className="flex gap-3 px-4 py-3 text-sm text-red-700"
                  >
                    <XCircle className="mt-0.5 h-4 w-4 shrink-0" />

                    <span>
                      {issue.message}
                    </span>
                  </div>
                )
              )}
            </div>
          </div>
        ) : (
          <div className="flex gap-3 rounded-lg border border-green-200 bg-green-50 p-4 text-green-700">
            <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0" />

            <div>
              <div className="font-medium">
                Ready to generate
              </div>

              <p className="mt-1 text-sm">
                Class requirements,
                teacher coverage,
                timetable capacity and
                staff availability have
                passed the MVP readiness
                checks.
              </p>
            </div>
          </div>
        )}

        <div className="grid gap-4 border-t pt-5 lg:grid-cols-[1fr_auto] lg:items-end">
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Academic Term
            </label>

            <select
              value={
                selectedTermId
              }
              onChange={(event) =>
                setSelectedTermId(
                  event.target.value
                )
              }
              className="h-10 w-full rounded-md border bg-background px-3 text-sm lg:max-w-md"
            >
              {terms.length === 0 ? (
                <option value="">
                  No terms configured
                </option>
              ) : (
                terms.map((term) => (
                  <option
                    key={term.id}
                    value={term.id}
                  >
                    {term.name}
                  </option>
                ))
              )}
            </select>
          </div>

          <Button
            type="button"
            onClick={generate}
            disabled={
              generating ||
              !readiness.canGenerate ||
              !selectedTermId
            }
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            {generating ? (
              <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
            ) : timetable ? (
              <RefreshCw className="mr-2 h-4 w-4" />
            ) : (
              <Sparkles className="mr-2 h-4 w-4" />
            )}

            {timetable
              ? "Regenerate Timetable"
              : "Generate Timetable"}
          </Button>
          {timetable && <Button type="button" variant="outline" onClick={() => void resetGenerated()} disabled={resetting || generating}>{resetting ? "Resetting…" : "Reset Generated Timetable"}</Button>}
        </div>

        {generationError && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {generationError}
          </div>
        )}

        {loadingExisting ? (
          <div className="flex items-center justify-center border-t py-12">
            <LoaderCircle className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : timetable ? (
          <GeneratedTimetableViewer
            timetable={timetable}
            settings={settings}
            viewMode={viewMode}
            setViewMode={
              setViewMode
            }
            classes={classes}
            teachers={teachers}
            selectedClassId={
              selectedClassId
            }
            setSelectedClassId={
              setSelectedClassId
            }
            selectedTeacherId={
              selectedTeacherId
            }
            setSelectedTeacherId={
              setSelectedTeacherId
            }
          />
        ) : (
          <div className="border-t py-12 text-center">
            <CalendarCheck2 className="mx-auto h-9 w-9 text-muted-foreground" />

            <h3 className="mt-3 font-medium">
              No timetable generated
              for this term
            </h3>

            <p className="mx-auto mt-1 max-w-lg text-sm text-muted-foreground">
              Once all readiness checks
              pass, generate the term
              timetable here.
            </p>
          </div>
        )}
      </div>
    </section>
  );
}

function ReadinessMetric({
  label,
  value,
  good,
}: {
  label: string;
  value: string;
  good: boolean;
}) {
  return (
    <div className="rounded-lg border p-4">
      <div className="flex items-center justify-between gap-3">
        <div className="text-xs text-muted-foreground">
          {label}
        </div>

        {good ? (
          <CheckCircle2 className="h-4 w-4 text-green-600" />
        ) : (
          <XCircle className="h-4 w-4 text-red-500" />
        )}
      </div>

      <div className="mt-2 text-xl font-bold">
        {value}
      </div>
    </div>
  );
}

function GeneratedTimetableViewer({
  timetable,
  settings,
  viewMode,
  setViewMode,
  classes,
  teachers,
  selectedClassId,
  setSelectedClassId,
  selectedTeacherId,
  setSelectedTeacherId,
}: {
  timetable: GeneratedTimetable;
  settings: TimetableSettings | null;
  viewMode: ViewMode;
  setViewMode: (
    value: ViewMode
  ) => void;
  classes: {
    id: string;
    name: string;
  }[];
  teachers: {
    id: string;
    name: string;
  }[];
  selectedClassId: string;
  setSelectedClassId: (
    value: string
  ) => void;
  selectedTeacherId: string;
  setSelectedTeacherId: (
    value: string
  ) => void;
}) {
  const visibleEntries =
    timetable.entries.filter(
      (entry) =>
        viewMode === "class"
          ? entry.classGroupId ===
            selectedClassId
          : entry.staffMemberId ===
            selectedTeacherId
    );

  const grouped =
    groupByDay(
      visibleEntries
    );

  return (
    <div className="space-y-5 border-t pt-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h3 className="font-semibold">
            Generated Timetable
          </h3>

          <p className="mt-1 text-xs text-muted-foreground">
            Generated{" "}
            {formatGeneratedDate(
              timetable.generatedAtUtc
            )}
            {" · "}
            {timetable.entryCount}{" "}
            lessons scheduled
          </p>
        </div>

        <div className="inline-flex rounded-lg border p-1">
          <button
            type="button"
            onClick={() =>
              setViewMode("class")
            }
            className={
              viewMode === "class"
                ? "inline-flex items-center rounded-md bg-muted px-3 py-2 text-sm font-medium"
                : "inline-flex items-center rounded-md px-3 py-2 text-sm text-muted-foreground"
            }
          >
            <UsersRound className="mr-2 h-4 w-4" />
            Class
          </button>

          <button
            type="button"
            onClick={() =>
              setViewMode(
                "teacher"
              )
            }
            className={
              viewMode === "teacher"
                ? "inline-flex items-center rounded-md bg-muted px-3 py-2 text-sm font-medium"
                : "inline-flex items-center rounded-md px-3 py-2 text-sm text-muted-foreground"
            }
          >
            <UserRound className="mr-2 h-4 w-4" />
            Teacher
          </button>
        </div>
      </div>

      {viewMode === "class" ? (
        <div className="space-y-2">
          <label className="text-sm font-medium">
            Class
          </label>

          <select
            value={
              selectedClassId
            }
            onChange={(event) =>
              setSelectedClassId(
                event.target.value
              )
            }
            className="h-10 w-full rounded-md border bg-background px-3 text-sm sm:max-w-md"
          >
            {classes.map(
              (item) => (
                <option
                  key={item.id}
                  value={item.id}
                >
                  {item.name}
                </option>
              )
            )}
          </select>
        </div>
      ) : (
        <div className="space-y-2">
          <label className="text-sm font-medium">
            Teacher
          </label>

          <select
            value={
              selectedTeacherId
            }
            onChange={(event) =>
              setSelectedTeacherId(
                event.target.value
              )
            }
            className="h-10 w-full rounded-md border bg-background px-3 text-sm sm:max-w-md"
          >
            {teachers.map(
              (item) => (
                <option
                  key={item.id}
                  value={item.id}
                >
                  {item.name}
                </option>
              )
            )}
          </select>
        </div>
      )}

      <div className="grid gap-4 xl:grid-cols-2">
        {grouped.map(
          ({
            day,
            entries,
          }) => {
            const blocks =
              getBlocksForDay(
                settings,
                day
              );

            return (
              <div
                key={day}
                className="overflow-hidden rounded-lg border"
              >
                <div className="border-b bg-muted/40 px-4 py-3 font-semibold">
                  {day}
                </div>

                {blocks.length >
                  0 && (
                  <div className="flex flex-wrap gap-2 border-b px-4 py-3">
                    {blocks.map(
                      (block) => (
                        <span
                          key={
                            block.id
                          }
                          className="rounded-full bg-amber-50 px-2.5 py-1 text-xs text-amber-700"
                        >
                          {block.name}
                          {" · "}
                          {formatTime(
                            block.startTime
                          )}
                          {"–"}
                          {formatTime(
                            block.endTime
                          )}
                        </span>
                      )
                    )}
                  </div>
                )}

                {entries.length ===
                0 ? (
                  <div className="p-5 text-sm text-muted-foreground">
                    No lessons
                    scheduled.
                  </div>
                ) : (
                  <div className="divide-y">
                    {entries.map(
                      (entry) => (
                        <div
                          key={
                            entry.id
                          }
                          className="grid grid-cols-[115px_1fr] gap-3 px-4 py-3"
                        >
                          <div className="text-xs text-muted-foreground">
                            <div className="font-medium text-foreground">
                              Period{" "}
                              {
                                entry.periodNumber
                              }
                            </div>

                            <div className="mt-1">
                              {formatTime(
                                entry.startTime
                              )}
                              {"–"}
                              {formatTime(
                                entry.endTime
                              )}
                            </div>
                          </div>

                          <div>
                            <div className="text-sm font-medium">
                              {
                                entry.subjectName
                              }
                            </div>

                            <div className="mt-1 text-xs text-muted-foreground">
                              {viewMode ===
                              "class"
                                ? entry.staffName
                                : `${entry.academicLevelName} — ${entry.classGroupName}`}
                            </div>
                          </div>
                        </div>
                      )
                    )}
                  </div>
                )}
              </div>
            );
          }
        )}
      </div>
    </div>
  );
}

function groupByDay(
  entries: GeneratedTimetableEntry[]
) {
  const days = [
    "Monday",
    "Tuesday",
    "Wednesday",
    "Thursday",
    "Friday",
    "Saturday",
    "Sunday",
  ];

  return days
    .map((day) => ({
      day,
      entries: entries
        .filter(
          (entry) =>
            dayName(
              entry.dayOfWeek
            ) === day
        )
        .sort(
          (a, b) =>
            a.periodNumber -
            b.periodNumber
        ),
    }))
    .filter(
      (item) =>
        item.entries.length > 0
    );
}

function dayName(
  value: number | string
) {
  if (
    typeof value === "string" &&
    Number.isNaN(Number(value))
  ) {
    return value;
  }

  const map: Record<
    number,
    string
  > = {
    0: "Sunday",
    1: "Monday",
    2: "Tuesday",
    3: "Wednesday",
    4: "Thursday",
    5: "Friday",
    6: "Saturday",
  };

  return (
    map[Number(value)] ??
    "Unknown"
  );
}

function getBlocksForDay(
  settings: TimetableSettings | null,
  day: string
) {
  if (!settings) {
    return [];
  }

  return (
    settings.days.find(
      (item) =>
        dayName(
          item.dayOfWeek
        ) === day
    )?.nonTeachingBlocks ??
    []
  );
}

function formatTime(
  value: string
) {
  return value.slice(0, 5);
}

function formatGeneratedDate(
  value: string
) {
  return new Intl.DateTimeFormat(
    "en-GB",
    {
      day: "numeric",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    }
  ).format(new Date(value));
}
