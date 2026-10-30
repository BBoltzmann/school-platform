"use client";

import { useEffect, useState } from "react";

export type TermSchedule = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  firstHalfWeight: number;
  secondHalfWeight: number;
  finalExamWeight: number;
  firstHalfDueDate: string | null;
  midtermBreakStartDate: string | null;
  resumeDate: string | null;
  midtermReportDueDate: string | null;
  secondHalfDueDate: string | null;
  finalExamDueDate: string | null;
  finalReportDueDate: string | null;
};

export const scheduleStages: { key: keyof TermSchedule; label: string }[] = [
  { key: "firstHalfDueDate", label: "First-half CA due" },
  { key: "midtermReportDueDate", label: "Midterm report due" },
  { key: "midtermBreakStartDate", label: "Midterm break begins" },
  { key: "resumeDate", label: "Students resume" },
  { key: "secondHalfDueDate", label: "Second-half CA due" },
  { key: "finalExamDueDate", label: "Final exams due" },
  { key: "finalReportDueDate", label: "Final report due" },
];

export function termDay(today: Date = new Date()) {
  return today.toLocaleDateString("en-CA", {
    timeZone: "Africa/Lagos", year: "numeric", month: "2-digit", day: "2-digit",
  });
}

export function dueStatus(date: string, today: string) {
  // ISO local calendar dates compare lexicographically, without UTC drift.
  if (date < today) return "Date passed";
  if (date === today) return "Due today";
  const left = Date.parse(`${today}T12:00:00Z`);
  const right = Date.parse(`${date}T12:00:00Z`);
  return (right - left) / 86400000 <= 7 ? "Due soon" : "Upcoming";
}

export function TermScheduleNotice({ termId, refreshKey = 0 }: { termId: string; refreshKey?: number }) {
  const [schedule, setSchedule] = useState<TermSchedule | null>(null);
  const [error, setError] = useState(false);
  useEffect(() => {
    if (!termId) return;
    const abort = new AbortController();
    // Clear old term before loading a different one.
    queueMicrotask(() => { if (!abort.signal.aborted) { setSchedule(null); setError(false); } });
    fetch(`/api/academics/terms/${encodeURIComponent(termId)}/schedule`, {
      signal: abort.signal, cache: "no-store",
    }).then(async response => {
      if (!response.ok) throw new Error("Schedule unavailable");
      return response.json() as Promise<TermSchedule>;
    }).then(data => { if (!abort.signal.aborted) setSchedule(data); })
      .catch(() => { if (!abort.signal.aborted) setError(true); });
    return () => abort.abort();
  }, [termId, refreshKey]);

  if (!termId || error || !schedule) return null;
  const today = termDay();
  const dates = scheduleStages.filter(stage => schedule[stage.key] != null)
    .map(stage => ({ ...stage, date: schedule[stage.key] as string }));
  if (dates.length === 0) return (
    <p className="text-xs text-muted-foreground">Assessment deadlines not configured for this term yet.</p>
  );

  return <section className="rounded-lg border bg-background p-3 text-sm" aria-label="Term assessment deadlines">
    <div className="font-medium">Assessment timetable · {schedule.firstHalfWeight}% / {schedule.secondHalfWeight}% / {schedule.finalExamWeight}%</div>
    <div className="mt-2 grid gap-2 sm:grid-cols-2">
      {dates.map(({ key, label, date }) => {
        const status = dueStatus(date, today);
        return <div key={key} className="flex items-center justify-between gap-2 rounded-md bg-muted/40 px-2 py-1.5">
          <span>{label}: <strong>{date}</strong></span>
          <span className={status === "Date passed" ? "font-semibold text-red-700" :
            status === "Due soon" || status === "Due today" ? "font-semibold text-amber-700" : "text-muted-foreground"}>
            {status}
          </span>
        </div>;
      })}
    </div>
    <p className="mt-2 text-xs text-muted-foreground">Past dates are not proof of missing work; verify saved marks separately. Marking and approval remain separate.</p>
  </section>;
}
