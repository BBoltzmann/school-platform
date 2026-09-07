"use client";

import {
  useEffect,
  useMemo,
  useState,
} from "react";
import {
  BookOpenCheck,
  CheckCircle2,
  ClipboardList,
  LoaderCircle,
  Plus,
  Save,
  Trash2,
  X,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type {
  Assessment,
  AssessmentGradebook,
  AssessmentScoreSheet,
  AssessmentSetup,
} from "@/types/assessments";

const ASSESSMENT_TYPES = [
  "Test",
  "Assignment",
  "Homework",
  "Project",
  "Quiz",
  "Exam",
  "Other",
];

export function AssessmentWorkspace({
  setup,
}: {
  setup: AssessmentSetup;
}) {
  const [
    academicTermId,
    setAcademicTermId,
  ] = useState(
    setup.terms[0]?.id ?? ""
  );

  const [
    classGroupId,
    setClassGroupId,
  ] = useState(
    setup.classes[0]?.id ?? ""
  );

  const [
    subjectId,
    setSubjectId,
  ] = useState(
    setup.subjects[0]?.id ?? ""
  );

  const [
    assessments,
    setAssessments,
  ] = useState<Assessment[]>([]);

  const [
    gradebook,
    setGradebook,
  ] = useState<
    AssessmentGradebook | null
  >(null);

  const [
    scoreSheet,
    setScoreSheet,
  ] = useState<
    AssessmentScoreSheet | null
  >(null);

  const [
    scores,
    setScores,
  ] = useState<
    Record<string, string>
  >({});

  const [
    showCreate,
    setShowCreate,
  ] = useState(false);

  const [
    loading,
    setLoading,
  ] = useState(false);

  const [
    saving,
    setSaving,
  ] = useState(false);

  const [
    error,
    setError,
  ] = useState<
    string | null
  >(null);

  const [
    notice,
    setNotice,
  ] = useState<
    string | null
  >(null);

  const [
    assessmentType,
    setAssessmentType,
  ] = useState("Test");

  const [
    assessmentTitle,
    setAssessmentTitle,
  ] = useState("");

  const [
    maximumScore,
    setMaximumScore,
  ] = useState("20");

  const [
    weightPercentage,
    setWeightPercentage,
  ] = useState("10");

  const selectionComplete =
    Boolean(
      academicTermId &&
        classGroupId &&
        subjectId
    );

  useEffect(() => {
    if (!selectionComplete) {
      setAssessments([]);
      setGradebook(null);
      setScoreSheet(null);
      return;
    }

    void loadWorkspace();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    academicTermId,
    classGroupId,
    subjectId,
  ]);

  async function loadWorkspace() {
    setLoading(true);
    setError(null);
    setNotice(null);
    setScoreSheet(null);

    try {
      const query =
        new URLSearchParams({
          academicTermId,
          classGroupId,
          subjectId,
        });

      const assessmentResponse =
        await fetch(
          `/api/assessments?${query}`
        );

      const assessmentResult =
        await assessmentResponse.json();

      if (!assessmentResponse.ok) {
        throw new Error(
          assessmentResult.error ??
            "Unable to load assessments."
        );
      }

      setAssessments(
        assessmentResult
      );

      if (
        assessmentResult.length === 0
      ) {
        setGradebook(null);
        return;
      }

      const gradebookResponse =
        await fetch(
          `/api/assessments/gradebook?${query}`
        );

      const gradebookResult =
        await gradebookResponse.json();

      if (!gradebookResponse.ok) {
        throw new Error(
          gradebookResult.error ??
            "Unable to load gradebook."
        );
      }

      setGradebook(
        gradebookResult
      );
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to load assessments."
      );
    } finally {
      setLoading(false);
    }
  }

  async function createAssessment() {
    const max =
      Number(maximumScore);

    const weight =
      Number(weightPercentage);

    if (
      !assessmentTitle.trim()
    ) {
      setError(
        "Enter an assessment title."
      );
      return;
    }

    if (
      !Number.isFinite(max) ||
      max <= 0
    ) {
      setError(
        "Maximum score must be greater than zero."
      );
      return;
    }

    if (
      !Number.isFinite(weight) ||
      weight <= 0 ||
      weight > 100
    ) {
      setError(
        "Weight must be between 0 and 100%."
      );
      return;
    }

    setSaving(true);
    setError(null);
    setNotice(null);

    try {
      const response =
        await fetch(
          "/api/assessments",
          {
            method: "POST",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify({
              academicTermId,
              classGroupId,
              subjectId,
              type:
                assessmentType,
              title:
                assessmentTitle.trim(),
              maximumScore: max,
              weightPercentage:
                weight,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ??
            "Unable to create assessment."
        );
      }

      setAssessmentTitle("");
      setMaximumScore("20");
      setWeightPercentage("10");
      setAssessmentType("Test");
      setShowCreate(false);

      setNotice(
        "Assessment created."
      );

      await loadWorkspace();

      await openScoreSheet(
        result.id
      );
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to create assessment."
      );
    } finally {
      setSaving(false);
    }
  }

  async function openScoreSheet(
    assessmentId: string
  ) {
    setLoading(true);
    setError(null);
    setNotice(null);

    try {
      const response =
        await fetch(
          `/api/assessments/${assessmentId}/scores`
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ??
            "Unable to load score sheet."
        );
      }

      const sheet =
        result as AssessmentScoreSheet;

      setScoreSheet(sheet);

      const initialScores:
        Record<string, string> =
        {};

      for (
        const student
        of sheet.students
      ) {
        initialScores[
          student.studentId
        ] =
          student.rawScore ===
          null
            ? ""
            : String(
                student.rawScore
              );
      }

      setScores(
        initialScores
      );
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to load score sheet."
      );
    } finally {
      setLoading(false);
    }
  }

  async function saveScores() {
    if (!scoreSheet) {
      return;
    }

    const max =
      scoreSheet.assessment
        .maximumScore;

    for (
      const student
      of scoreSheet.students
    ) {
      const value =
        scores[
          student.studentId
        ] ?? "";

      if (
        value.trim() === ""
      ) {
        continue;
      }

      const numeric =
        Number(value);

      if (
        !Number.isFinite(
          numeric
        ) ||
        numeric < 0 ||
        numeric > max
      ) {
        setError(
          `${student.studentName}: score must be between 0 and ${max}.`
        );
        return;
      }
    }

    setSaving(true);
    setError(null);
    setNotice(null);

    try {
      const response =
        await fetch(
          `/api/assessments/${scoreSheet.assessment.id}/scores`,
          {
            method: "PUT",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify({
              scores:
                scoreSheet.students.map(
                  (student) => {
                    const value =
                      scores[
                        student
                          .studentId
                      ] ?? "";

                    return {
                      studentId:
                        student.studentId,
                      rawScore:
                        value.trim() ===
                        ""
                          ? null
                          : Number(
                              value
                            ),
                    };
                  }
                ),
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ??
            "Unable to save scores."
        );
      }

      setScoreSheet(
        result
      );

      setNotice(
        "Scores saved."
      );

      await refreshGradebook();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to save scores."
      );
    } finally {
      setSaving(false);
    }
  }

  async function deleteAssessment(
    assessment: Assessment
  ) {
    const confirmed =
      window.confirm(
        `Delete "${assessment.title}"? Existing scores for this assessment will no longer contribute to the result.`
      );

    if (!confirmed) {
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const response =
        await fetch(
          `/api/assessments/${assessment.id}`,
          {
            method: "DELETE",
          }
        );

      if (!response.ok) {
        let result:
          | {
              error?: string;
            }
          | undefined;

        try {
          result =
            await response.json();
        } catch {
          // No JSON.
        }

        throw new Error(
          result?.error ??
            "Unable to delete assessment."
        );
      }

      if (
        scoreSheet
          ?.assessment.id ===
        assessment.id
      ) {
        setScoreSheet(null);
      }

      setNotice(
        "Assessment removed."
      );

      await loadWorkspace();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to delete assessment."
      );
    } finally {
      setSaving(false);
    }
  }

  async function refreshGradebook() {
    const query =
      new URLSearchParams({
        academicTermId,
        classGroupId,
        subjectId,
      });

    const response =
      await fetch(
        `/api/assessments/gradebook?${query}`
      );

    const result =
      await response.json();

    if (response.ok) {
      setGradebook(
        result
      );
    }
  }

  const configuredWeight =
    assessments.reduce(
      (sum, item) =>
        sum +
        Number(
          item.weightPercentage
        ),
      0
    );

  const selectedClass =
    setup.classes.find(
      (item) =>
        item.id ===
        classGroupId
    );

  const selectedSubject =
    setup.subjects.find(
      (item) =>
        item.id === subjectId
    );

  const currentContribution =
    useMemo(() => {
      if (!scoreSheet) {
        return {};
      }

      const result: Record<
        string,
        {
          percentage:
            number | null;
          contribution:
            number | null;
        }
      > = {};

      const max =
        scoreSheet.assessment
          .maximumScore;

      const weight =
        scoreSheet.assessment
          .weightPercentage;

      for (
        const student
        of scoreSheet.students
      ) {
        const value =
          scores[
            student.studentId
          ] ?? "";

        if (
          value.trim() === ""
        ) {
          result[
            student.studentId
          ] = {
            percentage: null,
            contribution: null,
          };

          continue;
        }

        const raw =
          Number(value);

        if (
          !Number.isFinite(raw)
        ) {
          result[
            student.studentId
          ] = {
            percentage: null,
            contribution: null,
          };

          continue;
        }

        result[
          student.studentId
        ] = {
          percentage:
            round2(
              (raw / max) *
                100
            ),
          contribution:
            round2(
              (raw / max) *
                weight
            ),
        };
      }

      return result;
    }, [
      scoreSheet,
      scores,
    ]);

  return (
    <div className="space-y-6">
      <section className="rounded-xl border bg-card p-5 shadow-sm">
        <div className="grid gap-4 lg:grid-cols-3">
          <Field
            label="Academic Term"
          >
            <select
              value={
                academicTermId
              }
              onChange={(
                event
              ) =>
                setAcademicTermId(
                  event.target
                    .value
                )
              }
              className="h-10 w-full rounded-md border bg-background px-3 text-sm"
            >
              {setup.terms.map(
                (term) => (
                  <option
                    key={term.id}
                    value={
                      term.id
                    }
                  >
                    {term.name}
                  </option>
                )
              )}
            </select>
          </Field>

          <Field label="Class">
            <select
              value={
                classGroupId
              }
              onChange={(
                event
              ) =>
                setClassGroupId(
                  event.target
                    .value
                )
              }
              className="h-10 w-full rounded-md border bg-background px-3 text-sm"
            >
              {setup.classes.map(
                (item) => (
                  <option
                    key={item.id}
                    value={
                      item.id
                    }
                  >
                    {
                      item.academicLevelName
                    }{" "}
                    —{" "}
                    {item.name}
                  </option>
                )
              )}
            </select>
          </Field>

          <Field label="Subject">
            <select
              value={subjectId}
              onChange={(
                event
              ) =>
                setSubjectId(
                  event.target
                    .value
                )
              }
              className="h-10 w-full rounded-md border bg-background px-3 text-sm"
            >
              {setup.subjects.map(
                (item) => (
                  <option
                    key={item.id}
                    value={
                      item.id
                    }
                  >
                    {item.name}
                  </option>
                )
              )}
            </select>
          </Field>
        </div>
      </section>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          {error}
        </div>
      )}

      {notice && (
        <div className="flex items-center gap-2 rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-700">
          <CheckCircle2 className="h-4 w-4" />
          {notice}
        </div>
      )}

      <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="flex flex-col gap-3 border-b p-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="font-semibold">
              Assessments
            </h2>

            <p className="mt-1 text-xs text-muted-foreground">
              {selectedClass
                ? `${selectedClass.academicLevelName} — ${selectedClass.name}`
                : "Select class"}
              {selectedSubject
                ? ` · ${selectedSubject.name}`
                : ""}
            </p>
          </div>

          <Button
            type="button"
            onClick={() =>
              setShowCreate(
                true
              )
            }
            disabled={
              !selectionComplete
            }
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            <Plus className="mr-2 h-4 w-4" />
            Add Assessment
          </Button>
        </div>

        <div className="grid gap-3 border-b p-5 sm:grid-cols-3">
          <Metric
            label="Assessments"
            value={String(
              assessments.length
            )}
          />

          <Metric
            label="Configured Weight"
            value={`${formatNumber(
              configuredWeight
            )}%`}
          />

          <Metric
            label="Result Status"
            value={
              configuredWeight ===
              100
                ? "Complete"
                : `${formatNumber(
                    Math.max(
                      0,
                      100 -
                        configuredWeight
                    )
                  )}% remaining`
            }
          />
        </div>

        {showCreate && (
          <div className="border-b bg-muted/20 p-5">
            <div className="mb-4 flex items-center justify-between">
              <div>
                <h3 className="font-medium">
                  New Assessment
                </h3>

                <p className="mt-1 text-xs text-muted-foreground">
                  Maximum score is
                  what the work is
                  marked over. Weight
                  is how much it
                  contributes to the
                  final result.
                </p>
              </div>

              <button
                type="button"
                onClick={() =>
                  setShowCreate(
                    false
                  )
                }
                className="rounded-md p-2 hover:bg-muted"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <Field label="Type">
                <select
                  value={
                    assessmentType
                  }
                  onChange={(
                    event
                  ) =>
                    setAssessmentType(
                      event.target
                        .value
                    )
                  }
                  className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                >
                  {ASSESSMENT_TYPES.map(
                    (type) => (
                      <option
                        key={
                          type
                        }
                        value={
                          type
                        }
                      >
                        {type}
                      </option>
                    )
                  )}
                </select>
              </Field>

              <Field label="Title">
                <input
                  value={
                    assessmentTitle
                  }
                  onChange={(
                    event
                  ) =>
                    setAssessmentTitle(
                      event.target
                        .value
                    )
                  }
                  placeholder="e.g. Test 1"
                  className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                />
              </Field>

              <Field label="Maximum Score">
                <input
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={
                    maximumScore
                  }
                  onChange={(
                    event
                  ) =>
                    setMaximumScore(
                      event.target
                        .value
                    )
                  }
                  className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                />
              </Field>

              <Field label="Weight (%)">
                <input
                  type="number"
                  min="0.01"
                  max="100"
                  step="0.01"
                  value={
                    weightPercentage
                  }
                  onChange={(
                    event
                  ) =>
                    setWeightPercentage(
                      event.target
                        .value
                    )
                  }
                  className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                />
              </Field>
            </div>

            <div className="mt-4">
              <Button
                type="button"
                onClick={
                  createAssessment
                }
                disabled={saving}
              >
                {saving ? (
                  <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Plus className="mr-2 h-4 w-4" />
                )}

                Create Score Sheet
              </Button>
            </div>
          </div>
        )}

        {loading ? (
          <div className="flex justify-center py-12">
            <LoaderCircle className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : assessments.length ===
          0 ? (
          <div className="py-12 text-center">
            <ClipboardList className="mx-auto h-9 w-9 text-muted-foreground" />

            <h3 className="mt-3 font-medium">
              No assessments yet
            </h3>

            <p className="mt-1 text-sm text-muted-foreground">
              Add a test,
              assignment, project,
              quiz or final exam.
            </p>
          </div>
        ) : (
          <div className="divide-y">
            {assessments.map(
              (assessment) => (
                <div
                  key={
                    assessment.id
                  }
                  className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-medium">
                        {
                          assessment.title
                        }
                      </span>

                      <span className="rounded-full bg-muted px-2 py-1 text-xs text-muted-foreground">
                        {
                          assessment.type
                        }
                      </span>
                    </div>

                    <div className="mt-2 text-xs text-muted-foreground">
                      Marked over{" "}
                      {formatNumber(
                        assessment.maximumScore
                      )}
                      {" · "}
                      Weight{" "}
                      {formatNumber(
                        assessment.weightPercentage
                      )}
                      %
                    </div>
                  </div>

                  <div className="flex gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() =>
                        openScoreSheet(
                          assessment.id
                        )
                      }
                    >
                      Enter Scores
                    </Button>

                    <Button
                      type="button"
                      variant="ghost"
                      onClick={() =>
                        deleteAssessment(
                          assessment
                        )
                      }
                    >
                      <Trash2 className="h-4 w-4 text-red-500" />
                    </Button>
                  </div>
                </div>
              )
            )}
          </div>
        )}
      </section>

      {scoreSheet && (
        <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
          <div className="flex flex-col gap-3 border-b p-5 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="font-semibold">
                {
                  scoreSheet
                    .assessment
                    .title
                }{" "}
                Score Sheet
              </h2>

              <p className="mt-1 text-xs text-muted-foreground">
                Maximum{" "}
                {formatNumber(
                  scoreSheet
                    .assessment
                    .maximumScore
                )}
                {" · "}
                Weight{" "}
                {formatNumber(
                  scoreSheet
                    .assessment
                    .weightPercentage
                )}
                %
              </p>
            </div>

            <Button
              type="button"
              onClick={
                saveScores
              }
              disabled={saving}
              className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
            >
              {saving ? (
                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Save className="mr-2 h-4 w-4" />
              )}

              Save Scores
            </Button>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full min-w-[780px] text-sm">
              <thead className="border-b bg-muted/30 text-left">
                <tr>
                  <th className="px-4 py-3 font-medium">
                    Student
                  </th>

                  <th className="px-4 py-3 font-medium">
                    Admission No.
                  </th>

                  <th className="px-4 py-3 font-medium">
                    Raw Score
                  </th>

                  <th className="px-4 py-3 font-medium">
                    Percentage
                  </th>

                  <th className="px-4 py-3 font-medium">
                    Contribution
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y">
                {scoreSheet.students.map(
                  (student) => {
                    const calculated =
                      currentContribution[
                        student
                          .studentId
                      ];

                    return (
                      <tr
                        key={
                          student.studentId
                        }
                      >
                        <td className="px-4 py-3 font-medium">
                          {
                            student.studentName
                          }
                        </td>

                        <td className="px-4 py-3 text-muted-foreground">
                          {
                            student.admissionNumber
                          }
                        </td>

                        <td className="px-4 py-3">
                          <div className="flex items-center gap-2">
                            <input
                              type="number"
                              min="0"
                              max={
                                scoreSheet
                                  .assessment
                                  .maximumScore
                              }
                              step="0.01"
                              value={
                                scores[
                                  student
                                    .studentId
                                ] ??
                                ""
                              }
                              onChange={(
                                event
                              ) =>
                                setScores(
                                  (
                                    current
                                  ) => ({
                                    ...current,
                                    [student.studentId]:
                                      event
                                        .target
                                        .value,
                                  })
                                )
                              }
                              className="h-9 w-24 rounded-md border bg-background px-2"
                            />

                            <span className="text-xs text-muted-foreground">
                              /{" "}
                              {formatNumber(
                                scoreSheet
                                  .assessment
                                  .maximumScore
                              )}
                            </span>
                          </div>
                        </td>

                        <td className="px-4 py-3">
                          {calculated?.percentage ===
                          null
                            ? "—"
                            : `${formatNumber(
                                calculated?.percentage ??
                                  0
                              )}%`}
                        </td>

                        <td className="px-4 py-3 font-medium">
                          {calculated?.contribution ===
                          null
                            ? "—"
                            : `${formatNumber(
                                calculated?.contribution ??
                                  0
                              )} / ${formatNumber(
                                scoreSheet
                                  .assessment
                                  .weightPercentage
                              )}`}
                        </td>
                      </tr>
                    );
                  }
                )}
              </tbody>
            </table>
          </div>
        </section>
      )}

      <GradebookPanel
        gradebook={gradebook}
      />
    </div>
  );
}

function GradebookPanel({
  gradebook,
}: {
  gradebook:
    | AssessmentGradebook
    | null;
}) {
  if (!gradebook) {
    return null;
  }

  return (
    <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className="flex flex-col gap-3 border-b p-5 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <BookOpenCheck className="h-5 w-5" />

            <h2 className="font-semibold">
              Subject Gradebook
            </h2>
          </div>

          <p className="mt-1 text-xs text-muted-foreground">
            {
              gradebook.classGroupName
            }
            {" · "}
            {
              gradebook.subjectName
            }
          </p>
        </div>

        <div
          className={
            gradebook.isComplete
              ? "rounded-full bg-green-50 px-3 py-1.5 text-xs font-medium text-green-700"
              : "rounded-full bg-amber-50 px-3 py-1.5 text-xs font-medium text-amber-700"
          }
        >
          {formatNumber(
            gradebook.configuredWeightTotal
          )}
          /100% configured
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full min-w-[900px] text-sm">
          <thead className="border-b bg-muted/30">
            <tr>
              <th className="sticky left-0 bg-muted/30 px-4 py-3 text-left font-medium">
                Student
              </th>

              {gradebook.assessments.map(
                (assessment) => (
                  <th
                    key={
                      assessment.assessmentId
                    }
                    className="px-4 py-3 text-center font-medium"
                  >
                    <div>
                      {
                        assessment.title
                      }
                    </div>

                    <div className="mt-1 text-[11px] font-normal text-muted-foreground">
                      {
                        assessment.weightPercentage
                      }
                      %
                    </div>
                  </th>
                )
              )}

              <th className="px-4 py-3 text-center font-semibold">
                Total
              </th>
            </tr>
          </thead>

          <tbody className="divide-y">
            {gradebook.students.map(
              (student) => (
                <tr
                  key={
                    student.studentId
                  }
                >
                  <td className="sticky left-0 bg-card px-4 py-3">
                    <div className="font-medium">
                      {
                        student.studentName
                      }
                    </div>

                    <div className="mt-1 text-xs text-muted-foreground">
                      {
                        student.admissionNumber
                      }
                    </div>
                  </td>

                  {gradebook.assessments.map(
                    (assessment) => {
                      const score =
                        student.scores.find(
                          (
                            item
                          ) =>
                            item.assessmentId ===
                            assessment.assessmentId
                        );

                      return (
                        <td
                          key={
                            assessment.assessmentId
                          }
                          className="px-4 py-3 text-center"
                        >
                          {score?.weightedContribution ===
                            null ||
                          score?.weightedContribution ===
                            undefined
                            ? "—"
                            : formatNumber(
                                score.weightedContribution
                              )}
                        </td>
                      );
                    }
                  )}

                  <td className="px-4 py-3 text-center text-base font-bold">
                    {formatNumber(
                      student.totalWeightedScore
                    )}
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>

      {!gradebook.isComplete && (
        <div className="border-t bg-amber-50 px-5 py-3 text-sm text-amber-700">
          The result is not yet
          complete. Assessment weights
          must total 100% before final
          term results can be
          published.
        </div>
      )}
    </section>
  );
}

function Field({
  label,
  children,
}: {
  label: string;
  children:
    React.ReactNode;
}) {
  return (
    <div className="space-y-2">
      <label className="text-sm font-medium">
        {label}
      </label>

      {children}
    </div>
  );
}

function Metric({
  label,
  value,
}: {
  label: string;
  value: string;
}) {
  return (
    <div className="rounded-lg border p-4">
      <div className="text-xs text-muted-foreground">
        {label}
      </div>

      <div className="mt-2 text-xl font-bold">
        {value}
      </div>
    </div>
  );
}

function round2(
  value: number
) {
  return Math.round(
    value * 100
  ) / 100;
}

function formatNumber(
  value: number
) {
  return new Intl.NumberFormat(
    "en-GB",
    {
      maximumFractionDigits:
        2,
    }
  ).format(value);
}
