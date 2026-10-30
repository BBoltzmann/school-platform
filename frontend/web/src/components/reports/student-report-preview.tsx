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
  projectedPercentage: number | null;
  projectedGrade: string | null;
  classAverage: number | null;
  classAverageCount: number;
  differenceFromClassAverage: number | null;
  advice: string;
};
type Report = {
  studentId: string;
  studentName: string;
  admissionNumber: string;
  status: string;
  className: string;
  classGroupId: string;
  academicSessionId: string;
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
  parallelGroups: {
    groupId: string; groupName: string; selectedSubjectId: string | null;
    options: { subjectId: string; subjectName: string }[];
  }[];
  canManageSubjectChoices: boolean;
  canCorrectScores: boolean;
  voidedMarks: {
    academicAssessmentId: string; rawScore: number; voidReason: string | null;
    voidedAtUtc: string | null; assessmentTitle: string; subjectName: string;
  }[];
  overallProjection: number | null;
  overallProjectedGrade: string | null;
  classOverallAverage: number | null;
  classOverallCount: number;
  overallDifferenceFromClass: number | null;
  unmatchedRecordedSubjects: { subjectId: string; subjectName: string; marks: Mark[] }[];
  firstHalfWeight: number;
  secondHalfWeight: number;
  finalExamWeight: number;
  subjectsAtFirstHalfWeight: number;
  provisionalMidtermReady: boolean;
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
  const [saving, setSaving] = useState(false);
  const [choices, setChoices] = useState<Record<string, string>>({});

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
      if (!signal.aborted) {
        setReport(result as Report);
        const selections: Record<string, string> = {};
        for (const group of (result as Report).parallelGroups ?? [])
          selections[group.groupId] = group.selectedSubjectId ?? "";
        setChoices(selections);
      }
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

  async function saveChoice(groupId: string) {
    if (!report || !choices[groupId]) return;
    setSaving(true);
    setError(null);
    try {
      const response = await fetch(`/api/students/${encodeURIComponent(id)}/parallel-subject-choice`, {
        method: "PUT", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ academicSessionId: report.academicSessionId,
          classGroupId: report.classGroupId, parallelSubjectGroupId: groupId,
          subjectId: choices[groupId] }),
      });
      if (!response.ok) throw new Error((await response.json().catch(() => ({}))).error ?? "Unable to save subject choice.");
      await load(termId, new AbortController().signal);
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to save subject choice."); }
    finally { setSaving(false); }
  }

  async function correctMark(assessmentId: string, action: "void" | "restore") {
    if (!report) return;
    const reason = window.prompt(action === "void"
      ? "Reason for removing this incorrect score (required, at least 5 characters):"
      : "Reason for restoring the score (required, at least 5 characters):");
    if (!reason || reason.trim().length < 5) return;
    if (!window.confirm(action === "void"
      ? "Remove this score from the report? It will remain in the audit history and can be restored."
      : "Restore this saved score to the report?")) return;
    setSaving(true);
    setError(null);
    try {
      const response = await fetch(`/api/students/${encodeURIComponent(id)}/report-scores/${encodeURIComponent(assessmentId)}/${action}`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ reason: reason.trim() }),
      });
      if (!response.ok) throw new Error((await response.json().catch(() => ({}))).error ?? "Score correction failed.");
      await load(termId, new AbortController().signal);
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Score correction failed."); }
    finally { setSaving(false); }
  }

  return (
    <main className="space-y-6 print:space-y-2 print:text-black">
      <style>{`@media print { @page { size: A4 portrait; margin: 11mm; } body { background: white !important; } }`}</style>
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
            <Printer className="mr-2 h-4 w-4" />{report?.provisionalMidtermReady ? "Print Midterm Draft" : "Print Draft"}
          </Button>
        </div>
      </header>
      {loading && <p role="status" className="rounded-lg border p-4">Loading assessment scores…</p>}
      {error && <div role="alert" className="rounded-lg border border-red-300 bg-red-50 p-4 text-red-800">{error}</div>}
      {report && !loading && <>
        <section className="rounded-xl border bg-card p-5">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Provisional midterm report — internal draft, not an official report</p>
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
        <section className={`rounded-lg border p-4 text-sm print:hidden ${report.provisionalMidtermReady
          ? "border-green-300 bg-green-50 text-green-950"
          : "border-amber-300 bg-amber-50 text-amber-950"}`}>
          <div className="flex items-center gap-2 font-semibold">
            {report.provisionalMidtermReady
              ? "First-half (midterm) draft: all configured marks recorded"
              : <><AlertTriangle className="h-4 w-4" />First-half assessment needs review</>}
          </div>
          <p className="mt-1">Default term breakdown: first-half assessment {report.firstHalfWeight}% +
            second-half assessment {report.secondHalfWeight}% + final exam {report.finalExamWeight}%.
            The final exam may be entered out of 100 and weighted to 60%.</p>
          <p className="mt-1">{report.subjectsAtFirstHalfWeight} of {report.totalSubjects} offered subjects currently have
            exactly {report.firstHalfWeight}% of assessment weight configured.</p>
          {report.provisionalMidtermReady && <p className="mt-2">This student's first-half assessment marks are complete
            under the default 20% checkpoint. You may print a midterm draft.</p>}
          {report.problems.length > 0 && <ul className="mt-2 list-disc space-y-1 pl-5">{report.problems.map(item => <li key={item}>{item}</li>)}</ul>}
          <p className="mt-2 text-xs">This checkpoint is provisional: individual assessments are not yet formally
            labelled Midterm or Final. An approved report and guardian publication remain unavailable.</p>
        </section>
        {report.parallelGroups.length > 0 && <section className="rounded-lg border p-4 print:hidden">
          <h3 className="font-semibold">Individual parallel-subject selections</h3>
          <p className="mt-1 text-sm text-muted-foreground">Select only the subject this student actually takes in each timetable group. Existing marks are not altered.</p>
          {report.parallelGroups.map(group => <div key={group.groupId} className="mt-3 flex flex-wrap items-center gap-3 border-t pt-3">
            <span className="min-w-44 text-sm font-medium">{group.groupName}</span>
            <select className="rounded-md border bg-background p-2 text-sm" aria-label={`Subject choice for ${group.groupName}`}
              disabled={!report.canManageSubjectChoices || saving}
              value={choices[group.groupId] ?? ""}
              onChange={event => setChoices(previous => ({ ...previous, [group.groupId]: event.target.value }))}>
              <option value="">Not yet confirmed</option>
              {group.options.map(option => <option key={option.subjectId} value={option.subjectId}>{option.subjectName}</option>)}
            </select>
            {report.canManageSubjectChoices && <Button size="sm" variant="outline" disabled={saving || !choices[group.groupId] ||
              choices[group.groupId] === (group.selectedSubjectId ?? "")}
              onClick={() => void saveChoice(group.groupId)}>Confirm selection</Button>}
            {!group.selectedSubjectId && <span className="text-xs text-amber-700">Needs confirmation</span>}
          </div>)}
        </section>}
        {report.unmatchedRecordedSubjects.length > 0 &&
          <section className="rounded-lg border border-amber-300 p-4 print:hidden">
            <h3 className="font-semibold">Saved marks requiring subject-offering review</h3>
            <p className="mt-1 text-sm text-muted-foreground">These marks are preserved but are not included in the confirmed class subjects.</p>
            {report.unmatchedRecordedSubjects.map(subject =>
              <div key={subject.subjectId} className="mt-3 border-t pt-2 text-sm">
                <p className="font-medium">{subject.subjectName}</p>
                {subject.marks.map(mark => <div key={mark.assessmentId} className="flex items-center justify-between gap-2 py-1">
                  <span>{mark.title}: {scoreText(mark.rawScore, mark.maximumScore)} ({mark.weightPercentage}% weight)</span>
                  {report.canCorrectScores && <Button size="xs" variant="destructive" disabled={saving}
                    onClick={() => void correctMark(mark.assessmentId, "void")}>Remove incorrect mark</Button>}
                </div>)}
              </div>)}
          </section>}
        {report.voidedMarks?.length > 0 && <section className="rounded-lg border p-4 print:hidden">
          <h3 className="font-semibold">Removed marks — recoverable audit history</h3>
          {report.voidedMarks.map(mark => <div key={mark.academicAssessmentId} className="mt-2 flex flex-wrap items-center gap-3 border-t pt-2 text-sm">
            <span className="flex-1">{mark.subjectName} · {mark.assessmentTitle} · {mark.rawScore} marks · {mark.voidReason ?? "No reason recorded"}</span>
            {report.canCorrectScores && <Button size="xs" variant="outline" disabled={saving}
              onClick={() => void correctMark(mark.academicAssessmentId, "restore")}>Restore mark</Button>}
          </div>)}
        </section>}
        <section className="break-inside-avoid rounded-lg border p-4 print:border-black print:p-2">
          <h3 className="font-semibold">Provisional midterm performance</h3>
          <p className="mt-1 text-xs text-muted-foreground">Projected grades require all configured first-half marks. A missing score is never zero.</p>
          <div className="mt-3 overflow-x-auto"><table className="w-full border-collapse text-xs sm:text-sm">
            <thead><tr className="border-b text-left">
              <th className="py-2 pr-2">Subject</th><th className="py-2 pr-2">CA /20</th>
              <th className="py-2 pr-2">Projected</th><th className="py-2 pr-2">Grade</th>
              <th className="py-2 pr-2">Class avg</th><th className="py-2">Comparison</th>
            </tr></thead>
            <tbody>{report.subjects.map(subject => <tr key={subject.subjectId} className="border-b">
              <td className="py-2 pr-2">{subject.subjectName}</td>
              <td className="py-2 pr-2">{subject.marksComplete ? subject.totalWeightedScore : "Incomplete"}</td>
              <td className="py-2 pr-2">{subject.projectedPercentage === null ? "—" : `${subject.projectedPercentage}%`}</td>
              <td className="py-2 pr-2">{subject.projectedGrade ?? "—"}</td>
              <td className="py-2 pr-2">{subject.classAverage === null ? "—" : `${subject.classAverage}% (n=${subject.classAverageCount})`}</td>
              <td className="py-2">{subject.differenceFromClassAverage === null ? "—" :
                `${Math.abs(subject.differenceFromClassAverage)} points ${subject.differenceFromClassAverage < 0 ? "below" : "above"} average`}</td>
            </tr>)}</tbody>
          </table></div>
          <p className="mt-3 text-sm font-semibold">Overall projection: {report.overallProjection === null
            ? "Incomplete" : `${report.overallProjection}% (${report.overallProjectedGrade})`}</p>
          <p className="mt-1 text-sm">Comparable class overall average: {report.classOverallAverage === null
            ? "Not available yet" : `${report.classOverallAverage}% (n=${report.classOverallCount})`}
            {report.overallDifferenceFromClass !== null &&
              ` · ${Math.abs(report.overallDifferenceFromClass)} points ${report.overallDifferenceFromClass < 0 ? "below" : "above"} average`}</p>
          <p className="mt-1 text-xs text-muted-foreground">Projection is based only on first-half assessment performance; it is not a final examination score. Overall comparisons include only students on the same confirmed subject track with complete first-half marks.</p>
          <div className="mt-3 space-y-2">
            {report.subjects.filter(s => s.projectedPercentage !== null && s.classAverage !== null).map(subject =>
              <div key={subject.subjectId} className="break-inside-avoid text-xs">
                <p>{subject.subjectName} — student {subject.projectedPercentage}% · class {subject.classAverage}%</p>
                <div className="mt-1 h-2 w-full bg-muted print:border">
                  <div className="h-2 bg-slate-700 print:bg-black" style={{ width: `${Math.max(0, Math.min(100, subject.projectedPercentage ?? 0))}%` }} />
                </div>
              </div>)}
          </div>
        </section>
        {report.subjects.some(subject => subject.projectedGrade === "D" || subject.projectedGrade === "F") &&
          <section className="break-inside-avoid rounded-lg border p-4 print:p-2">
            <h3 className="font-semibold">Parent / guardian action</h3>
            {report.subjects.filter(s => s.projectedGrade === "D" || s.projectedGrade === "F").map(subject =>
              <p className="mt-2 text-sm" key={subject.subjectId}><strong>{subject.subjectName}:</strong> {subject.advice}</p>)}
          </section>}
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
            {subject.projectedPercentage !== null && <p className="mt-1 text-sm">Projected midterm: {subject.projectedPercentage}% · Grade {subject.projectedGrade} · {subject.advice}</p>}
            {report.canCorrectScores && subject.assessments.some(mark => mark.rawScore !== null) &&
              <div className="mt-2 flex flex-wrap gap-2 print:hidden">{subject.assessments.filter(mark => mark.rawScore !== null).map(mark =>
                <Button key={mark.assessmentId} size="xs" variant="destructive" disabled={saving}
                  onClick={() => void correctMark(mark.assessmentId, "void")}>Remove {mark.title} score</Button>)}</div>}
          </article>)}
        </section>
        <footer className="border-t pt-4 text-xs text-muted-foreground">
          Internal assessment draft. Not approved, certified, or released to a parent or guardian. A missing score is not zero.
        </footer>
      </>}
    </main>
  );
}
