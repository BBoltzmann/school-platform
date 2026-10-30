"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Printer, ArrowLeft } from "lucide-react";
import styles from "./parent-midterm-report.module.css";

type AssessmentMark = {
  assessmentId: string;
  title: string;
  maximumScore: number;
  rawScore: number | null;
};
type SubjectSummary = {
  subjectId: string;
  subjectName: string;
  marksComplete: boolean;
  totalWeightedScore: number;
  assessments: AssessmentMark[];
  projectedPercentage: number | null;
  projectedGrade: string | null;
  classAverage: number | null;
  classAverageCount: number;
  differenceFromClassAverage: number | null;
};
type ReportSummary = {
  studentName: string;
  admissionNumber: string;
  className: string;
  sessionName: string;
  academicTermName: string;
  subjects: SubjectSummary[];
  overallProjection: number | null;
  overallProjectedGrade: string | null;
  classOverallAverage: number | null;
  classOverallCount: number;
  overallDifferenceFromClass: number | null;
  provisionalMidtermReady: boolean;
  hasUnresolvedElectives: boolean;
  problems: string[];
};

function number(value: number): string {
  return Number.isInteger(value) ? String(value) : value.toFixed(2).replace(/0+$/, "").replace(/\.$/, "");
}

/** Human-readable checkpoint feedback; independent of stored assessment weight. */
export function assessmentComment(mark: AssessmentMark): string {
  if (mark.rawScore === null || mark.maximumScore <= 0) return "Not recorded";
  const percent = 100 * mark.rawScore / mark.maximumScore;
  if (percent >= 70) return "Excellent";
  if (percent >= 60) return "Very good";
  if (percent >= 50) return "Satisfactory";
  if (percent >= 40) return "Needs improvement";
  return "Poor — requires support";
}

function subjectComment(subject: SubjectSummary): string {
  if (subject.projectedPercentage === null || !subject.projectedGrade) return "Awaiting marks";
  switch (subject.projectedGrade) {
    case "A": return "Excellent progress";
    case "B": return "Very good progress";
    case "C": return "Satisfactory progress";
    case "D": return "Discuss improvement with teacher";
    case "F": return "Teacher meeting advised";
    default: return "Review with teacher";
  }
}

function gradeColor(grade: string | null): string {
  switch (grade) {
    case "A": return "#157347";
    case "B": return "#2776a6";
    case "C": return "#b67f1b";
    case "D": return "#c46b25";
    case "F": return "#b33131";
    default: return "#9ca3af";
  }
}

/** Explicit SVG fills are retained by browser Print-to-PDF even with background graphics disabled. */
function PerformanceBar({ subject }: { subject: SubjectSummary }) {
  const value = Math.max(0, Math.min(100, subject.projectedPercentage ?? 0));
  const average = subject.classAverage === null ? null : Math.max(0, Math.min(100, subject.classAverage));
  return <svg className={styles.bar} viewBox="0 0 100 8" preserveAspectRatio="none"
    aria-label={`Student projection ${subject.projectedPercentage ?? "unavailable"} percent${average === null ? "" : `, class average ${subject.classAverage} percent`}`} role="img">
    <rect x="0" y="0" width="100" height="8" fill="#e3e7ed" />
    <rect x="0" y="0" width={value} height="8" fill={gradeColor(subject.projectedGrade)} />
    {average !== null && <rect x={Math.max(0, average - 0.7)} y="0" width="1.4" height="8" fill="#111827" />}
  </svg>;
}

function SubjectTable({ subjects }: { subjects: SubjectSummary[] }) {
  return <table className={styles.table}>
    <colgroup><col style={{ width: "34%" }} /><col style={{ width: "10%" }} /><col style={{ width: "11%" }} />
      <col style={{ width: "8%" }} /><col style={{ width: "13%" }} /><col style={{ width: "24%" }} /></colgroup>
    <thead><tr><th>Subject / assessment feedback</th><th>CA /20</th><th>Projection</th><th>Grade</th><th>Class avg.</th><th>Teacher guidance</th></tr></thead>
    <tbody>{subjects.map(subject => <tr key={subject.subjectId}>
      <td><strong>{subject.subjectName}</strong>
        <span className={styles.assessmentList}>
          {subject.assessments.length === 0 ? "No tests recorded" : subject.assessments.map(mark =>
            `${mark.title}: ${mark.rawScore === null ? "—" : `${number(mark.rawScore)}/${number(mark.maximumScore)}`} (${assessmentComment(mark)})`
          ).join(" · ")}
        </span>
      </td>
      <td className={styles.numeric}>{subject.projectedPercentage === null ? "—" : number(subject.totalWeightedScore)}</td>
      <td className={styles.numeric}>
        <strong>{subject.projectedPercentage === null ? "—" : `${number(subject.projectedPercentage)}%`}</strong>
        {subject.projectedPercentage !== null && <PerformanceBar subject={subject} />}
      </td>
      <td className={styles.grade} style={{ color: gradeColor(subject.projectedGrade) }}>{subject.projectedGrade ?? "—"}</td>
      <td className={styles.numeric}>{subject.classAverage === null ? "—" : <>{number(subject.classAverage)}%<span className={styles.small}>({subject.classAverageCount} pupils)</span></>}</td>
      <td>{subjectComment(subject)}
        {subject.differenceFromClassAverage !== null && <span className={styles.small}>
          {Math.abs(subject.differenceFromClassAverage) < 0.005 ? "At class average" :
            `${number(Math.abs(subject.differenceFromClassAverage))} points ${subject.differenceFromClassAverage < 0 ? "below" : "above"} average`}
        </span>}
      </td>
    </tr>)}</tbody>
  </table>;
}

function PageHeader({ report, schoolName, logoDataUrl, page, totalPages }: {
  report: ReportSummary; schoolName: string; logoDataUrl?: string | null; page: number; totalPages: number;
}) {
  return <>
    <header className={styles.schoolHeader}>
      <div className={styles.schoolIdentity}>
        {logoDataUrl ? <img className={styles.logo} src={logoDataUrl} alt="School logo" /> : <div className={styles.monogram}>SCH</div>}
        <div><div className={styles.schoolName}>{schoolName}</div><div className={styles.schoolSub}>Student academic progress</div></div>
      </div>
      <div className={styles.reportHeading}><strong>Midterm Progress Report</strong><span>PROVISIONAL · FIRST-HALF ASSESSMENT</span></div>
    </header>
    <div className={styles.studentDetails}>
      <div><span>Student</span><strong>{report.studentName}</strong></div>
      <div><span>Admission no.</span><strong>{report.admissionNumber || "—"}</strong></div>
      <div><span>Class</span><strong>{report.className}</strong></div>
      <div><span>Session / term</span><strong>{report.sessionName} · {report.academicTermName}</strong></div>
    </div>
    <div className={styles.tableHeading}><strong>Subject performance</strong><span>Page {page} of {totalPages} · Comparisons use pupils taking the same subject</span></div>
  </>;
}

function ReportSummaryFooter({ report }: { report: ReportSummary }) {
  const interventions = report.subjects.filter(s => s.projectedGrade === "D" || s.projectedGrade === "F");
  return <section className={styles.summary}>
    <div className={styles.summaryNumbers}>
      <div><span>Overall projected performance</span><strong>{report.overallProjection === null ? "Incomplete" : `${number(report.overallProjection)}% · ${report.overallProjectedGrade ?? "—"}`}</strong></div>
      <div><span>Comparable class average</span><strong>{report.classOverallAverage === null ? "Not available" : `${number(report.classOverallAverage)}% (${report.classOverallCount} pupils)`}</strong></div>
      <div><span>Overall comparison</span><strong>{report.overallDifferenceFromClass === null ? "Not available" :
        `${number(Math.abs(report.overallDifferenceFromClass))} points ${report.overallDifferenceFromClass < 0 ? "below" : "above"} average`}</strong></div>
    </div>
    {interventions.length > 0 && <div className={styles.interventions}>
      <strong>Parent / guardian follow-up</strong>
      <p>Please contact the relevant subject teachers to discuss support and an improvement plan for: {interventions.map(s => s.subjectName).join(", ")}.</p>
    </div>}
    {!report.provisionalMidtermReady && <p className={styles.incompleteNote}>Some results or subject selections still require confirmation; this report may be incomplete.</p>}
    <p className={styles.disclaimer}>This is a provisional midterm progress report based on saved first-half assessments. Projected scores are not final-term examination results. No missing mark is treated as zero.</p>
  </section>;
}

export function ParentMidtermReport({ studentId, termId, schoolName, logoDataUrl, backHref }: {
  studentId: string; termId: string; schoolName: string; logoDataUrl?: string | null; backHref: string;
}) {
  const [report, setReport] = useState<ReportSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    const query = termId ? `?academicTermId=${encodeURIComponent(termId)}` : "";
    void (async () => {
      try {
        const response = await fetch(`/api/students/${encodeURIComponent(studentId)}/report-preview${query}`, {
          signal: controller.signal, cache: "no-store",
        });
        const data = await response.json();
        if (!response.ok) throw new Error(data.error ?? `Report unavailable (${response.status}).`);
        if (!controller.signal.aborted) { setReport(data as ReportSummary); setError(""); }
      } catch (cause) {
        if (!controller.signal.aborted) setError(cause instanceof Error ? cause.message : "Unable to load report.");
      } finally { if (!controller.signal.aborted) setLoading(false); }
    })();
    return () => controller.abort();
  }, [studentId, termId]);

  const subjects = report?.subjects ?? [];
  // Split approximately evenly; a typical 9–18 subject class occupies at most two A4 sheets.
  const firstPageCount = subjects.length <= 10 ? subjects.length : Math.ceil(subjects.length / 2);
  const pages = report && subjects.length > firstPageCount ? [subjects.slice(0, firstPageCount), subjects.slice(firstPageCount)] : [subjects];

  return <div className={styles.root}>
    <div className={styles.toolbar}>
      <Link href={backHref} className={styles.back}><ArrowLeft size={16} /> Return to detailed report</Link>
      <div><strong>Family-friendly print view</strong><span>Two-page A4 layout for typical class sizes. Select “Save as PDF” in the print dialog.</span></div>
      <button type="button" onClick={() => window.print()} disabled={loading || !report} className={styles.printButton}>
        <Printer size={16} /> Print / Save PDF
      </button>
    </div>
    {loading && <p role="status" className={styles.status}>Loading student results…</p>}
    {error && <p role="alert" className={styles.status}>{error}</p>}
    {report && !loading && <>
      {pages.map((pageSubjects, index) => <article className={`${styles.sheet} ${subjects.length > 18 ? styles.dense : ""}`} key={index}>
        <PageHeader report={report} schoolName={schoolName} logoDataUrl={logoDataUrl} page={index + 1} totalPages={pages.length} />
        <SubjectTable subjects={pageSubjects} />
        {index === pages.length - 1 && <ReportSummaryFooter report={report} />}
        {index !== pages.length - 1 && <div className={styles.continued}>Continued on page 2</div>}
        <div className={styles.pageFooter}>Prepared from recorded school assessments · {report.academicTermName} · Page {index + 1} of {pages.length}</div>
      </article>)}
    </>}
  </div>;
}
