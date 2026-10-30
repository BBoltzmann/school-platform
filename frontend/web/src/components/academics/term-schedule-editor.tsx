"use client";

import { FormEvent, useState } from "react";
import { Button } from "@/components/ui/button";
import { TermScheduleNotice, type TermSchedule } from "./term-schedule-notice";

export const scheduleDateFields = [
  ["firstHalfDueDate", "First-half CA deadline"],
  ["midtermBreakStartDate", "Midterm break starts"],
  ["resumeDate", "Resumption date"],
  ["midtermReportDueDate", "Midterm report due"],
  ["secondHalfDueDate", "Second-half CA deadline"],
  ["finalExamDueDate", "Final exam marks due"],
  ["finalReportDueDate", "Final report due"],
] as const;

export const defaultWeights = { firstHalfWeight: 20, secondHalfWeight: 20, finalExamWeight: 60 };

export function TermScheduleEditor({ term }: { term: { id: string; name: string; startDate: string; endDate: string } }) {
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<Partial<TermSchedule>>({ ...defaultWeights });
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [refreshKey, setRefreshKey] = useState(0);

  async function openEditor() {
    setMessage("");
    try {
      const response = await fetch(`/api/academics/terms/${term.id}/schedule`, { cache: "no-store" });
      if (!response.ok) throw new Error(`Unable to load schedule (${response.status})`);
      setForm(await response.json() as TermSchedule);
      setOpen(true);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Unable to load term schedule.");
    }
  }

  function update(field: keyof TermSchedule, value: string | number | null) {
    setForm(previous => ({ ...previous, [field]: value }));
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch(`/api/academics/terms/${term.id}/schedule`, {
        method: "PUT", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ...form, ...defaultWeights }),
      });
      if (!response.ok) {
        const result = await response.json().catch(() => ({}));
        throw new Error(result.error ?? `Unable to save (${response.status})`);
      }
      setOpen(false);
      setRefreshKey(n => n + 1);
      setMessage("Schedule saved.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Unable to save schedule.");
    } finally { setBusy(false); }
  }

  return <div className="mt-3 space-y-2">
    <TermScheduleNotice termId={term.id} refreshKey={refreshKey} />
    <Button size="sm" variant="outline" onClick={() => void openEditor()}>
      Edit assessment schedule
    </Button>
    {message && <p className="text-xs" role="status">{message}</p>}
    {open && <form onSubmit={save} className="mt-3 space-y-3 rounded-lg border p-3">
      <p className="text-sm font-semibold">{term.name}: term assessment calendar</p>
      <p className="text-xs text-muted-foreground">Term dates: {term.startDate} – {term.endDate}. Missing dates can be left blank.</p>
      <p className="text-xs font-medium">Default: 20% first-half CA · 20% second-half CA · 60% final exam</p>
      <div className="grid gap-2 sm:grid-cols-2">
        {scheduleDateFields.map(([key, label]) => <label key={key} className="text-xs">{label}
          <input type="date" className="mt-1 w-full rounded-md border p-2"
            value={form[key] ?? ""}
            onChange={event => update(key, event.target.value || null)}/>
        </label>)}
      </div>
      <div className="flex gap-2"><Button type="submit" size="sm" disabled={busy}>{busy ? "Saving…" : "Save schedule"}</Button>
        <Button type="button" size="sm" variant="outline" onClick={() => setOpen(false)}>Cancel</Button></div>
    </form>}
  </div>;
}
