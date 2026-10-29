"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { ArrowLeft, Printer, RefreshCcw, AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";

type Mark = {
  assessmentId: string;
  title: string;
  type: string;
  maximumScore: number;
  weightPercentage: number;
  rawScore: number | null;
  weightedContribution: number | null;
};
type Subject = {
  subjectId: string;
  subjectName: string;
  recordedScores: number;
  missingScores: number;
  configuredWeight: number;
  totalWeightedScore: number;
  marksComplete: boolean;
  assessments: Mark[];
};
type Report = {
  studentId: string;
  studentName: string;
  admissionNumber: string;
  status: string;
  className: string;
  sessionName: string;
  academicTermId: string;
  academicTermName: string;
  terms: { id: string; name: string }[];
  subjects: Subject[];
  totalSubjects: number;
  subjectsWithMarksComplete: number;
  totalRecordedMarks: number;
  totalMissingMarks: number;
  hasUnresolvedElectives: boolean;
  problems: string[];
  canGenerateOfficial: boolean;
};

function scoreText(value: number | null, maximum: number) {
  return value === null ? "Not recorded" : `${value} / ${maximum}`;
}

export function StudentReportPreview({
  studentId,
  backHref,
  backLabel,
}: {
  studentId: string;
  backHref: string;
  backLabel: string;
}) {
  const id = studentId;
  const [termId, setTermId] = useState("");
  const [report, setReport] = useState<Report | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async (selectedTerm: string, signal: AbortSignal) => {
    setLoading(true);
    setError(null);
    try {
      const query = selectedTerm ? `?academicTermId=${encodeURIComponent(selectedTerm)}` : "";
      const response = await fetch(`/api/students/${encodeURIComponent(id)}/report-preview${query}`, {
        cache: "no-store", signal,
      });
      const result = await response.json();
      if (!response.ok) throw new Error(result.error ?? `Unable to load report (${response.status}).`);
      if (!signal.aborted) setReport(result as Report);
    } catch (cause) {
      if (!signal.aborted) {
        setReport(null);
        setError(cause instanceof Error ? cause.message : "Unable to load report.");
      }
    } finally {
      if (!signal.aborted) setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    const controller = new AbortController();
    void load(termId, controller.signal);
    return () => controller.abort();
  }, [load, termId]);

  return (
    <main className="space-y-6">
      <div className="print:hidden">
        <Link className="inline-flex items-center gap-2 text-sm underline" href={backHref}>
          <ArrowLeft className="h-4 w-4" /> {backLabel}
        </Link>
      </div>
      <header className="flex flex-col justify-between gap-3 sm:flex-row sm:items-end">
        <div>
          <h1 className="text-2xl font-bold">Student Report Card Preview</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Individual term-to-date marks, subject progress and missing-score checks.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2 print:hidden">
          <Button variant="outline" onClick={() => {
            const controller = new AbortController();
            void load(termId, controller.signal);
          }} disabled={loading}><RefreshCcw className="mr-2 h-4 w-4" />Refresh</Button>
          <Button onClick={() => window.print()} disabled={!report || loading}>
            <Printer className="mr-2 h-4 w-4" />Print Draft
          </Button>
        </div>
      </header>
      {loading && <p role="status" className="rounded-lg border p-4">Loading assessment scores…</p>}
      {error && <div role="alert" className="rounded-lg border border-red-300 bg-red-50 p-4 text-red-800">{error}</div>}
      {report && !loading && <>
        <section className="rounded-xl border bg-card p-5">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Internal working draft — not an official report</p>
              <h2 className="mt-2 text-xl font-semibold">{report.studentName}</h2>
              <p className="text-sm">{report.admissionNumber} · {report.className} · {report.status}</p>
              <p className="mt-1 text-sm text-muted-foreground">{report.sessionName} · {report.academicTermName}</p>
            </div>
            <div className="print:hidden">
              <label className="block text-xs font-medium" htmlFor="report-term">Academic term</label>
              <select id="report-term" className="mt-1 rounded-md border bg-background p-2 text-sm"
                value={report.academicTermId} onChange={event => setTermId(event.target.value)}>
                {report.terms.map(term => <option key={term.id} value={term.id}>{term.name}</option>)}
              </select>
            </div>
          </div>
          <div className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
            {[
              ["Subjects", report.totalSubjects],
              ["Marks entered", report.totalRecordedMarks],
              ["Missing marks", report.totalMissingMarks],
              ["Subjects with all saved marks", `${report.subjectsWithMarksComplete}/${report.totalSubjects}`],
            ].map(([title, value]) => <div key={title} className="rounded-lg bg-muted/50 p-3">
              <p className="text-xs text-muted-foreground">{title}</p>
              <p className="mt-1 text-lg font-bold">{value}</p>
            </div>)}
          </div>
        </section>
        <section className="rounded-lg border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950 print:border-black print:bg-white">
          <div className="flex items-center gap-2 font-semibold"><AlertTriangle className="h-4 w-4" />Not ready for official midterm or final publication</div>
          <p className="mt-1">This page displays all currently configured term assessments. It does not infer midterm versus final marks or publish results to guardians.</p>
          <ul className="mt-2 list-disc space-y-1 pl-5">{report.problems.map(item => <li key={item}>{item}</li>)}</ul>
        </section>
        {report.subjects.length === 0 && <p className="rounded-lg border p-5">No offered subjects found for this enrollment. Verify the class subject setup.</p>}
        <section className="space-y-4">
          {report.subjects.map(subject => <article key={subject.subjectId} className="break-inside-avoid rounded-xl border bg-card p-4 sm:p-5">
            <div className="flex flex-wrap items-start justify-between gap-2">
              <div><h3 className="font-semibold">{subject.subjectName}</h3>
                <p className="mt-1 text-xs text-muted-foreground">{subject.recordedScores} recorded · {subject.missingScores} missing · configured weight {subject.configuredWeight}%</p>
              </div>
              <span className={`rounded-full px-3 py-1 text-xs font-medium ${subject.marksComplete ? "bg-green-100 text-green-800" : "bg-amber-100 text-amber-900"}`}>
                {subject.marksComplete ? "All configured marks entered" : "Needs attention"}
              </span>
            </div>
            <div className="mt-4 space-y-2">
              {subject.assessments.length ? subject.assessments.map(mark => <div key={mark.assessmentId} className="grid grid-cols-[1fr_auto] gap-3 border-t py-2 text-sm sm:grid-cols-[2fr_1fr_1fr]">
                <div><span className="font-medium">{mark.title}</span> <span className="text-xs text-muted-foreground">({mark.type}, weight {mark.weightPercentage}%)</span></div>
                <div className="text-right sm:text-left">{scoreText(mark.rawScore, mark.maximumScore)}</div>
                <div className="text-right text-muted-foreground sm:text-left">{mark.weightedContribution === null ? "—" : `${mark.weightedContribution}% weighted`}</div>
              </div>) : <p className="text-sm text-amber-800">No assessment has been set up for this subject.</p>}
            </div>
            <p className="mt-3 border-t pt-3 text-sm font-semibold">Recorded weighted total: {subject.totalWeightedScore}% (term-to-date only)</p>
          </article>)}
        </section>
        <footer className="border-t pt-4 text-xs text-muted-foreground">
          Internal assessment draft. Not approved, certified, or released to a parent or guardian. A missing score is not zero.
        </footer>
      </>}
    </main>
  );
}
