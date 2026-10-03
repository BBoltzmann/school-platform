"use client";

import {
  useState,
} from "react";
import {
  LoaderCircle,
  Plus,
  Trash2,
  ReceiptText,
  Search,
  Users,
  ArrowLeft,
  X,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type {
  FeeStructure,
  FeeStructureStudent,
  FeesSetup,
} from "@/types/fees";

async function readFeeResponse(response: Response): Promise<Record<string, unknown>> {
  const body = await response.text();
  if (!body.trim()) {
    return {};
  }
  try {
    const parsed: unknown = JSON.parse(body);
    return parsed && typeof parsed === "object"
      ? parsed as Record<string, unknown>
      : {};
  } catch {
    return {};
  }
}

export function FeeStructuresWorkspace({
  setup,
}: {
  setup: FeesSetup;
}) {
  const [
    feeItems,
    setFeeItems,
  ] = useState(
    setup.feeItems
  );

  const [
    structures,
    setStructures,
  ] = useState(
    setup.structures
  );

  const [
    newFeeName,
    setNewFeeName,
  ] = useState("");

  const [
    structureName,
    setStructureName,
  ] = useState("");

  const [
    academicTermId,
    setAcademicTermId,
  ] = useState(
    setup.terms[0]?.id ??
      ""
  );

  const [
    audienceType,
    setAudienceType,
  ] = useState("Level");

  const [
    audienceId,
    setAudienceId,
  ] = useState("");

  const [lines, setLines] =
    useState<
      {
        feeItemId: string;
        amount: string;
        isRequired: boolean;
      }[]
    >([]);

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null
    );

  const [notice, setNotice] =
    useState<string | null>(
      null
    );

  const [selectedStructure, setSelectedStructure] =
    useState<FeeStructure | null>(null);
  const [activeView, setActiveView] = useState<"list" | "structure" | "students">("list");

  const [assignedStudents, setAssignedStudents] =
    useState<FeeStructureStudent[]>([]);

  const [selectedStudentIds, setSelectedStudentIds] =
    useState<string[]>([]);

  const [studentSearch, setStudentSearch] =
    useState("");

  const [studentClassFilter, setStudentClassFilter] =
    useState("");

  const [loadingAssignments, setLoadingAssignments] =
    useState(false);

  const [editingName, setEditingName] = useState("");
  const [editingAudienceType, setEditingAudienceType] = useState("School");
  const [editingAudienceId, setEditingAudienceId] = useState("");
  const [editingLines, setEditingLines] = useState<
    {
      id?: string;
      feeItemId: string;
      amount: string;
      isRequired: boolean;
    }[]
  >([]);

  const audienceOptions =
    audienceType === "Level"
      ? setup.levels
      : audienceType === "Class"
        ? setup.classes
        : [];

  async function createFeeItem() {
    setError(null);

    if (!newFeeName.trim()) {
      return;
    }

    setSaving(true);

    try {
      const response =
        await fetch(
          "/api/fees/items",
          {
            method: "POST",
            headers: {
              "Content-Type":
                "application/json",
            },
            body:
              JSON.stringify({
                name:
                  newFeeName.trim(),
                code: null,
                description:
                  null,
              }),
          }
        );

      const result = await response.json();

      if (!response.ok) {
        throw new Error(
          typeof result.error === "string"
            ? result.error
            : "Unable to create fee item."
        );
      }

      setFeeItems(
        current => [
          ...current,
          result,
        ]
      );

      setNewFeeName("");
      setNotice(
        "Fee item created."
      );
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to create fee item."
      );
    } finally {
      setSaving(false);
    }
  }

  async function createStructure() {
    setError(null);
    setNotice(null);

    if (
      !structureName.trim() ||
      !academicTermId ||
      lines.length === 0
    ) {
      setError(
        "Complete the fee structure and add at least one fee."
      );
      return;
    }

    if (
      audienceType !==
        "School" &&
      !audienceId
    ) {
      setError(
        "Select a level or class."
      );
      return;
    }

    setSaving(true);

    try {
      const response =
        await fetch(
          "/api/fees/structures",
          {
            method: "POST",
            headers: {
              "Content-Type":
                "application/json",
            },
            body:
              JSON.stringify({
                academicTermId,
                name:
                  structureName.trim(),
                audienceType,
                audienceId:
                  audienceType ===
                  "School"
                    ? null
                    : audienceId,
                lines:
                  lines.map(
                    line => ({
                      feeItemId:
                        line.feeItemId,
                      amount:
                        Number(
                          line.amount
                        ),
                      isRequired:
                        line.isRequired,
                    })
                  ),
              }),
          }
        );

      const result = await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ?? "Unable to create fee structure."
        );
      }

      setStructures(
        current => [
          result,
          ...current,
        ]
      );

      setStructureName("");
      setLines([]);

      setNotice(
        "Fee structure created."
      );
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to create fee structure."
      );
    } finally {
      setSaving(false);
    }
  }

  async function generate(
    structure: FeeStructure
  ) {
    let assignmentCount =
      selectedStructure?.id === structure.id
        ? assignedStudents.length
        : 0;

    if (selectedStructure?.id !== structure.id) {
      try {
        const assignmentsResponse = await fetch(
          `/api/fees/structures/${structure.id}/students`
        );
        if (assignmentsResponse.ok) {
          const assignments = await assignmentsResponse.json();
          assignmentCount = Array.isArray(assignments)
            ? assignments.length
            : 0;
        }
      } catch {
        // The generation endpoint remains authoritative if the preview count
        // cannot be loaded.
      }
    }

    const targeting = `Explicit students (${assignmentCount})`;
    const termName = setup.terms.find(term => term.id === structure.academicTermId)?.name
      ?? structure.academicTermId;
    const confirmed = window.confirm(
      [
        `Generate / sync fees for ${structure.name}?`,
        `Term: ${termName}`,
        `Fee components: ${structure.lines.length}`,
        `Amount per student: ${currency(structure.totalRequiredAmount)}`,
        `Targeting: ${targeting}`,
        `Students targeted: ${assignmentCount}`,
      ].join("\n")
    );

    if (!confirmed) {
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const response =
        await fetch(
          `/api/fees/structures/${structure.id}/generate`,
          {
            method: "POST",
          }
        );

      const result = await readFeeResponse(response);

      if (!response.ok) {
        throw new Error(
          typeof result.error === "string"
            ? result.error
            : `Fee sync failed (HTTP ${response.status}).`
        );
      }

      setNotice(
        `Fees synchronised successfully — ${result.chargesCreated ?? 0} created, ${result.reactivatedCount ?? 0} reactivated, ${result.deactivatedCount ?? 0} obsolete charges removed, ${result.protectedCount ?? 0} protected.`
      );
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to generate charges."
      );
    } finally {
      setSaving(false);
    }
  }

  async function reconcileTermFees() {
    if (!academicTermId) return;
    const termName = setup.terms.find(term => term.id === academicTermId)?.name ?? academicTermId;
    if (!window.confirm(`Reconcile Term Fees for ${termName}?\n\nUnpaid generated charges that no longer apply will be deactivated. Paid and manual charges will be preserved.`)) {
      return;
    }

    setSaving(true);
    setError(null);
    try {
      const response = await fetch(`/api/fees/terms/${academicTermId}/reconcile`, { method: "POST" });
      const result = await readFeeResponse(response);
      if (!response.ok) {
        throw new Error(
          typeof result.error === "string"
            ? result.error
            : `Fee reconciliation failed (HTTP ${response.status}).`
        );
      }
      setNotice(`Term fees reconciled — ${result.chargesCreated ?? 0} created, ${result.chargesDeactivated ?? 0} deactivated, ${result.protectedCharges ?? 0} protected.`);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to reconcile term fees.");
    } finally {
      setSaving(false);
    }
  }

  async function openStructure(structure: FeeStructure, view: "structure" | "students") {
    setSelectedStructure(structure);
    setActiveView(view);
    setEditingName(structure.name);
    setEditingAudienceType(structure.audienceType);
    setEditingAudienceId(structure.audienceId ?? "");
    setEditingLines(
      structure.lines.map(line => ({
        id: line.id,
        feeItemId: line.feeItemId,
        amount: String(line.amount),
        isRequired: line.isRequired,
      }))
    );
    setStudentSearch("");
    setStudentClassFilter("");
    setLoadingAssignments(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/fees/structures/${structure.id}/students`
      );
      const result = await response.json();

      if (!response.ok) {
        throw new Error(result.error ?? "Unable to load assigned students.");
      }

      if (!Array.isArray(result)) {
        throw new Error(
          typeof result === "object" && result !== null && "error" in result && typeof result.error === "string"
            ? result.error
            : "The fee service did not confirm the saved student assignments."
        );
      }

      const assignments = result as FeeStructureStudent[];
      setAssignedStudents(assignments);
      setSelectedStudentIds(assignments.map(student => student.studentId));
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to load assigned students."
      );
    } finally {
      setLoadingAssignments(false);
    }
  }

  function closeStructure() {
    if (saving) return;
    setSelectedStructure(null);
    setActiveView("list");
    setError(null);
    setNotice(null);
  }

  async function saveStructure() {
    if (!selectedStructure) {
      return;
    }

    if (!editingName.trim() || editingLines.length === 0) {
      setError("A structure needs a name and at least one fee component.");
      return;
    }

    if (editingAudienceType !== "School" && !editingAudienceId) {
      setError("Select a level or class for this structure.");
      return;
    }

    setSaving(true);
    setError(null);
    setNotice(null);

    try {
      const response = await fetch(
        `/api/fees/structures/${selectedStructure.id}`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            name: editingName.trim(),
            audienceType: editingAudienceType,
            audienceId: editingAudienceType === "School"
              ? null
              : editingAudienceId,
            lines: editingLines.map(line => ({
              id: line.id ?? null,
              feeItemId: line.feeItemId,
              amount: Number(line.amount),
              isRequired: line.isRequired,
            })),
          }),
        }
      );
      const result = await response.json();

      if (!response.ok) {
        throw new Error(result.error ?? "Unable to update fee structure.");
      }

      setStructures(current => current.map(structure =>
        structure.id === result.id ? result : structure));
      setSelectedStructure(result);
      setEditingLines(result.lines.map((line: FeeStructure["lines"][number]) => ({
        id: line.id,
        feeItemId: line.feeItemId,
        amount: String(line.amount),
        isRequired: line.isRequired,
      })));
      setNotice("Fee structure template updated. Existing charges were not changed.");
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to update fee structure."
      );
    } finally {
      setSaving(false);
    }
  }

  async function saveAssignments() {
    if (!selectedStructure) {
      return;
    }

    setSaving(true);
    setError(null);
    setNotice(null);

    try {
      const response = await fetch(
        `/api/fees/structures/${selectedStructure.id}/students`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            studentIds: selectedStudentIds,
          }),
        }
      );
      const result = await response.json();

      if (!response.ok) {
        throw new Error(result.error ?? "Unable to save student assignments.");
      }

      if (!Array.isArray(result)) {
        throw new Error(
          typeof result === "object" && result !== null && "error" in result && typeof result.error === "string"
            ? result.error
            : "The fee service did not confirm the saved student assignments."
        );
      }

      const assignments = result as FeeStructureStudent[];
      setAssignedStudents(assignments);
      setSelectedStudentIds(assignments.map(student => student.studentId));
      setNotice(
        `${assignments.length} student${assignments.length === 1 ? "" : "s"} assigned to ${selectedStructure.name}.`
      );
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to save student assignments."
      );
    } finally {
      setSaving(false);
    }
  }

  async function removeAssignment(student: FeeStructureStudent) {
    if (!selectedStructure) return;

    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      const response = await fetch(
        `/api/fees/structures/${selectedStructure.id}/students/${student.studentId}`,
        { method: "DELETE" }
      );
      const result = await response.json().catch(() => ({}));
      if (!response.ok) {
        throw new Error(result.error ?? "Unable to remove student assignment.");
      }

      const refreshed = await fetch(
        `/api/fees/structures/${selectedStructure.id}/students`
      );
      const assignments = await refreshed.json();
      if (!refreshed.ok || !Array.isArray(assignments)) {
        throw new Error(assignments.error ?? "Unable to refresh assigned students.");
      }
      setAssignedStudents(assignments);
      setSelectedStudentIds(assignments.map((item: FeeStructureStudent) => item.studentId));
      setNotice(`${student.studentName} was removed from this fee structure. Run Generate / Sync Fees to reconcile unpaid generated charges.`);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to remove student assignment.");
    } finally {
      setSaving(false);
    }
  }

  const classOptions = Array.from(
    new Set(
      setup.students
        .map(student => student.className)
        .filter((value): value is string => Boolean(value))
    )
  ).sort();

  const filteredStudents = setup.students.filter(student => {
    const query = studentSearch.trim().toLowerCase();
    const matchesSearch = !query ||
      student.name.toLowerCase().includes(query) ||
      student.admissionNumber.toLowerCase().includes(query);
    const matchesClass = !studentClassFilter ||
      student.className === studentClassFilter;
    const alreadyAssigned = assignedStudents.some(assigned => assigned.studentId === student.id);
    return matchesSearch && matchesClass && !alreadyAssigned;
  });

  return (
    <div className="space-y-6">
      {error && (
        <Message type="error">
          {error}
        </Message>
      )}

      {notice && (
        <Message type="success">
          {notice}
        </Message>
      )}

      {activeView === "list" && <>
      <section className="rounded-xl border bg-card p-5">
        <h2 className="font-semibold">
          Fee Items
        </h2>

        <p className="mt-1 text-xs text-muted-foreground">
          Create the types of
          charges used by the school.
        </p>

        <div className="mt-4 flex max-w-xl gap-2">
          <input
            value={newFeeName}
            onChange={event =>
              setNewFeeName(
                event.target.value
              )
            }
            placeholder="e.g. Tuition"
            className={inputClass}
          />

          <Button
            onClick={
              createFeeItem
            }
            disabled={saving}
          >
            <Plus className="mr-2 h-4 w-4" />
            Add
          </Button>
        </div>

        <div className="mt-4 flex flex-wrap gap-2">
          {feeItems.map(item => (
            <span
              key={item.id}
              className="rounded-full bg-muted px-3 py-1.5 text-sm"
            >
              {item.name}
            </span>
          ))}
        </div>
      </section>
      </>}

      {activeView === "list" && <section className="rounded-xl border bg-card">
        <div className="border-b p-5">
          <h2 className="font-semibold">
            New Fee Structure
          </h2>

          <p className="mt-1 text-xs text-muted-foreground">
            Build the fees for a
            school, level or class.
          </p>
        </div>

        <div className="space-y-5 p-5">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <Field label="Structure Name">
              <input
                value={structureName}
                onChange={event =>
                  setStructureName(
                    event.target
                      .value
                  )
                }
                placeholder="First Term — Primary 3"
                className={
                  inputClass
                }
              />
            </Field>

            <Field label="Term">
              <select
                value={
                  academicTermId
                }
                onChange={event =>
                  setAcademicTermId(
                    event.target
                      .value
                  )
                }
                className={
                  inputClass
                }
              >
                {setup.terms.map(
                  term => (
                    <option
                      key={term.id}
                      value={term.id}
                    >
                      {term.name}
                    </option>
                  )
                )}
              </select>
            </Field>

            <Field label="Audience">
              <select
                value={
                  audienceType
                }
                onChange={event => {
                  setAudienceType(
                    event.target
                      .value
                  );
                  setAudienceId("");
                }}
                className={
                  inputClass
                }
              >
                <option value="School">
                  Entire School
                </option>

                <option value="Level">
                  Academic Level
                </option>

                <option value="Class">
                  Specific Class
                </option>
              </select>
            </Field>

            {audienceType !==
              "School" && (
              <Field
                label={
                  audienceType
                }
              >
                <select
                  value={
                    audienceId
                  }
                  onChange={event =>
                    setAudienceId(
                      event.target
                        .value
                    )
                  }
                  className={
                    inputClass
                  }
                >
                  <option value="">
                    Select
                  </option>

                  {audienceOptions.map(
                    option => (
                      <option
                        key={
                          option.id
                        }
                        value={
                          option.id
                        }
                      >
                        {
                          option.name
                        }
                      </option>
                    )
                  )}
                </select>
              </Field>
            )}
          </div>

          <div>
            <div className="mb-3 flex items-center justify-between">
              <div>
                <h3 className="text-sm font-medium">
                  Fee Components
                </h3>
              </div>

              <Button
                variant="outline"
                size="sm"
                disabled={
                  feeItems.length ===
                  0
                }
                onClick={() =>
                  setLines(
                    current => [
                      ...current,
                      {
                        feeItemId:
                          feeItems[0]
                            ?.id ??
                          "",
                        amount: "",
                        isRequired:
                          true,
                      },
                    ]
                  )
                }
              >
                <Plus className="mr-2 h-4 w-4" />
                Add Fee
              </Button>
            </div>

            <div className="space-y-2">
              {lines.map(
                (line, index) => (
                  <div
                    key={index}
                    className="grid gap-2 rounded-lg border p-3 md:grid-cols-[1fr_180px_160px_auto]"
                  >
                    <select
                      value={
                        line.feeItemId
                      }
                      onChange={event =>
                        setLines(
                          current =>
                            current.map(
                              (
                                item,
                                i
                              ) =>
                                i ===
                                index
                                  ? {
                                      ...item,
                                      feeItemId:
                                        event
                                          .target
                                          .value,
                                    }
                                  : item
                            )
                        )
                      }
                      className={
                        inputClass
                      }
                    >
                      {feeItems.map(
                        item => (
                          <option
                            key={
                              item.id
                            }
                            value={
                              item.id
                            }
                          >
                            {
                              item.name
                            }
                          </option>
                        )
                      )}
                    </select>

                    <input
                      type="number"
                      value={
                        line.amount
                      }
                      onChange={event =>
                        setLines(
                          current =>
                            current.map(
                              (
                                item,
                                i
                              ) =>
                                i ===
                                index
                                  ? {
                                      ...item,
                                      amount:
                                        event
                                          .target
                                          .value,
                                    }
                                  : item
                            )
                        )
                      }
                      placeholder="Amount"
                      className={
                        inputClass
                      }
                    />

                    <label className="flex h-10 items-center gap-2 rounded-md border px-3 text-sm">
                      <input
                        type="checkbox"
                        checked={
                          line.isRequired
                        }
                        onChange={event =>
                          setLines(
                            current =>
                              current.map(
                                (
                                  item,
                                  i
                                ) =>
                                  i ===
                                  index
                                    ? {
                                        ...item,
                                        isRequired:
                                          event
                                            .target
                                            .checked,
                                      }
                                    : item
                              )
                          )
                        }
                      />

                      Required
                    </label>

                    <Button
                      variant="ghost"
                      onClick={() =>
                        setLines(
                          current =>
                            current.filter(
                              (
                                _,
                                i
                              ) =>
                                i !==
                                index
                            )
                        )
                      }
                    >
                      Remove
                    </Button>
                  </div>
                )
              )}
            </div>
          </div>

          <Button
            onClick={
              createStructure
            }
            disabled={saving}
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            {saving && (
              <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
            )}

            Save Fee Structure
          </Button>
        </div>
      </section>}

      {activeView === "list" && <section className="rounded-xl border bg-card">
        <div className="border-b p-5">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <h2 className="font-semibold">Existing Fee Structures</h2>
            <Button type="button" variant="outline" onClick={reconcileTermFees} disabled={saving || !academicTermId}>
              Reconcile Term Fees
            </Button>
          </div>
        </div>

        {structures.length ===
        0 ? (
          <div className="p-10 text-center text-sm text-muted-foreground">
            No fee structures yet.
          </div>
        ) : (
          <div className="divide-y">
            {structures.map(
              structure => (
                <div
                  key={
                    structure.id
                  }
                  className="flex flex-col gap-4 p-5 lg:flex-row lg:items-center lg:justify-between"
                >
                  <div>
                    <div className="flex items-center gap-2">
                      <ReceiptText className="h-4 w-4" />

                      <span className="font-semibold">
                        {
                          structure.name
                        }
                      </span>
                    </div>

                    <div className="mt-2 text-xs text-muted-foreground">
                      {
                        structure.audienceName
                      }
                      {" · "}
                      {structure.lines.length}{" "}
                      components
                      {" · "}
                      {currency(
                        structure.totalRequiredAmount
                      )}{" "}
                      required
                    </div>
                  </div>

                  <Button
                    variant="outline"
                    onClick={() => openStructure(structure, "structure")}
                    disabled={saving}
                  >
                    <ReceiptText className="mr-2 h-4 w-4" />
                    Manage Structure
                  </Button>

                  <Button
                    variant="outline"
                    onClick={() => openStructure(structure, "students")}
                    disabled={saving}
                  >
                    <Users className="mr-2 h-4 w-4" />
                    Manage Students
                  </Button>

                  <Button
                    variant="outline"
                    onClick={() =>
                      generate(
                        structure
                      )
                    }
                    disabled={saving}
                  >
                    <Users className="mr-2 h-4 w-4" />
                    Generate / Sync Fees
                  </Button>
                </div>
              )
            )}
          </div>
        )}
      </section>}

      {selectedStructure && activeView !== "list" && (
        <section className="rounded-xl border bg-card p-5">
          <div className="flex flex-col gap-3 border-b pb-4 md:flex-row md:items-start md:justify-between">
            <div>
              <h2 className="font-semibold">
                {activeView === "structure" ? "Manage Structure" : "Manage Students"}: {selectedStructure.name}
              </h2>
              <p className="mt-1 text-xs text-muted-foreground">
                {selectedStructure.audienceName} audience · Charges are generated only for explicitly assigned students.
              </p>
            </div>
            <div className="flex items-center gap-3">
              <div className="rounded-full bg-muted px-3 py-1 text-sm">
                {assignedStudents.length} assigned
              </div>
              <Button type="button" variant="ghost" onClick={closeStructure}>
                <ArrowLeft className="mr-1 h-4 w-4" />
                Back to Fee Structures
              </Button>
            </div>
          </div>

          <div className="grid gap-4 border-b py-4 md:grid-cols-2 lg:grid-cols-4">
            <div>
              <div className="text-xs text-muted-foreground">Session</div>
              <div className="text-sm font-medium">
                {setup.currentSession?.name ?? selectedStructure.academicSessionId}
              </div>
            </div>
            <div>
              <div className="text-xs text-muted-foreground">Term</div>
              <div className="text-sm font-medium">
                {setup.terms.find(term => term.id === selectedStructure.academicTermId)?.name
                  ?? selectedStructure.academicTermId}
              </div>
            </div>
            <div>
              <div className="text-xs text-muted-foreground">Audience</div>
              <div className="text-sm font-medium">{selectedStructure.audienceType}</div>
            </div>
            <div>
              <div className="text-xs text-muted-foreground">Total required</div>
              <div className="text-sm font-medium">{currency(selectedStructure.totalRequiredAmount)}</div>
            </div>
          </div>

          {activeView === "structure" && <div className="border-b py-4">
            <h3 className="font-medium">Fee Components</h3>
            <div className="mt-2 overflow-x-auto rounded-lg border">
              <table className="w-full text-sm">
                <thead className="bg-muted/40 text-left text-xs uppercase text-muted-foreground">
                  <tr>
                    <th className="p-3">Component</th>
                    <th className="p-3 text-right">Amount</th>
                    <th className="p-3">Required</th>
                  </tr>
                </thead>
                <tbody>
                  {selectedStructure.lines.map(line => (
                    <tr key={line.id} className="border-t">
                      <td className="p-3">{line.feeItemName}</td>
                      <td className="p-3 text-right">{currency(line.amount)}</td>
                      <td className="p-3">{line.isRequired ? "Yes" : "Optional"}</td>
                    </tr>
                  ))}
                  <tr className="border-t font-semibold">
                    <td className="p-3">Total required</td>
                    <td className="p-3 text-right">{currency(selectedStructure.totalRequiredAmount)}</td>
                    <td />
                  </tr>
                </tbody>
              </table>
            </div>
          </div>}

          {activeView === "structure" && <div className="border-b py-4">
            <h3 className="font-medium">Edit Structure Template</h3>
            <p className="mt-1 text-xs text-muted-foreground">
              Changes are applied when fees are generated/synchronised. Unpaid obsolete charges may be removed from active billing; paid charges and payment history are preserved.
            </p>
            <div className="mt-3 grid gap-3 md:grid-cols-3">
              <Field label="Structure name">
                <input
                  value={editingName}
                  onChange={event => setEditingName(event.target.value)}
                  className={inputClass}
                />
              </Field>
              <Field label="Audience type">
                <select
                  value={editingAudienceType}
                  onChange={event => {
                    setEditingAudienceType(event.target.value);
                    setEditingAudienceId("");
                  }}
                  className={inputClass}
                >
                  <option value="School">School</option>
                  <option value="Level">Academic level</option>
                  <option value="Class">Class</option>
                </select>
              </Field>
              <Field label="Audience">
                <select
                  value={editingAudienceId}
                  onChange={event => setEditingAudienceId(event.target.value)}
                  disabled={editingAudienceType === "School"}
                  className={inputClass}
                >
                  <option value="">{editingAudienceType === "School" ? "Entire school" : "Select audience"}</option>
                  {(
                    editingAudienceType === "Level" ? setup.levels : setup.classes
                  ).map(option => (
                    <option key={option.id} value={option.id}>{option.name}</option>
                  ))}
                </select>
              </Field>
            </div>

            <div className="mt-4 space-y-3">
              {editingLines.map((line, index) => (
                <div key={line.id ?? `new-${index}`} className="grid gap-2 md:grid-cols-[1fr_160px_auto_auto] md:items-end">
                  <Field label="Fee component">
                    <select
                      value={line.feeItemId}
                      onChange={event => setEditingLines(current => current.map((item, itemIndex) =>
                        itemIndex === index ? { ...item, feeItemId: event.target.value } : item))}
                      className={inputClass}
                    >
                      <option value="">Select fee item</option>
                      {feeItems.map(item => (
                        <option key={item.id} value={item.id}>{item.name}</option>
                      ))}
                    </select>
                  </Field>
                  <Field label="Amount">
                    <input
                      type="number"
                      min="0.01"
                      step="0.01"
                      value={line.amount}
                      onChange={event => setEditingLines(current => current.map((item, itemIndex) =>
                        itemIndex === index ? { ...item, amount: event.target.value } : item))}
                      className={inputClass}
                    />
                  </Field>
                  <label className="flex h-10 items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      checked={line.isRequired}
                      onChange={event => setEditingLines(current => current.map((item, itemIndex) =>
                        itemIndex === index ? { ...item, isRequired: event.target.checked } : item))}
                    />
                    Required
                  </label>
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={() => setEditingLines(current => current.filter((_, itemIndex) => itemIndex !== index))}
                  >
                    <Trash2 className="mr-1 h-4 w-4" />
                    Remove
                  </Button>
                </div>
              ))}
            </div>

            <div className="mt-3 flex flex-wrap gap-3">
              <Button
                type="button"
                variant="outline"
                onClick={() => setEditingLines(current => [
                  ...current,
                  { feeItemId: "", amount: "", isRequired: true },
                ])}
              >
                <Plus className="mr-1 h-4 w-4" />
                Add Fee Component
              </Button>
              <Button
                type="button"
                onClick={saveStructure}
                disabled={saving}
                className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
              >
                Save Structure Changes
              </Button>
            </div>
          </div>}

          {activeView === "students" && (loadingAssignments ? (
            <div className="flex items-center gap-2 py-8 text-sm text-muted-foreground">
              <LoaderCircle className="h-4 w-4 animate-spin" />
              Loading assignments…
            </div>
          ) : (
            <>
              <div className="border-b py-4">
                <h3 className="font-medium">Assigned Students ({assignedStudents.length})</h3>
                {assignedStudents.length === 0 ? (
                  <p className="mt-2 text-sm text-muted-foreground">No students are explicitly assigned to this structure.</p>
                ) : (
                  <div className="mt-3 divide-y rounded-lg border">
                    {assignedStudents.map(student => (
                      <div key={student.studentId} className="flex items-center gap-3 p-3">
                        <div className="min-w-0 flex-1">
                          <div className="text-sm font-medium">{student.studentName}</div>
                          <div className="text-xs text-muted-foreground">
                            {student.admissionNumber}{student.className ? ` · ${student.className}` : ""}
                          </div>
                        </div>
                        <Button
                          type="button"
                          variant="ghost"
                          aria-label={`Remove ${student.studentName}`}
                          onClick={() => removeAssignment(student)}
                          disabled={saving}
                        >
                          <X className="mr-1 h-4 w-4" />
                          Remove
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <div className="pt-4">
                <h3 className="font-medium">Add Students</h3>
                <p className="mt-1 text-xs text-muted-foreground">Search below to add students to this fee structure.</p>
              </div>
              <div className="mt-4 grid gap-3 md:grid-cols-[1fr_220px_auto] md:items-end">
                <Field label="Search students">
                  <div className="relative">
                    <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                    <input
                      value={studentSearch}
                      onChange={event => setStudentSearch(event.target.value)}
                      placeholder="Name or admission number"
                      className={`${inputClass} pl-9`}
                    />
                  </div>
                </Field>

                <Field label="Class">
                  <select
                    value={studentClassFilter}
                    onChange={event => setStudentClassFilter(event.target.value)}
                    className={inputClass}
                  >
                    <option value="">All classes</option>
                    {classOptions.map(option => (
                      <option key={option} value={option}>{option}</option>
                    ))}
                  </select>
                </Field>

                <Button
                  variant="outline"
                  onClick={() => {
                    const visibleIds = filteredStudents.map(student => student.id);
                    setSelectedStudentIds(current => Array.from(new Set([...current, ...visibleIds])));
                  }}
                >
                  Select filtered
                </Button>
              </div>

              <div className="mt-4 max-h-80 overflow-y-auto rounded-lg border">
                {filteredStudents.length === 0 ? (
                  <div className="p-6 text-center text-sm text-muted-foreground">
                    No students match the current filters.
                  </div>
                ) : (
                  filteredStudents.map(student => (
                    <label
                      key={student.id}
                      className="flex cursor-pointer items-center gap-3 border-b p-3 last:border-b-0 hover:bg-muted/50"
                    >
                      <input
                        type="checkbox"
                        checked={selectedStudentIds.includes(student.id)}
                        onChange={event => {
                          setSelectedStudentIds(current => event.target.checked
                            ? [...current, student.id]
                            : current.filter(id => id !== student.id));
                        }}
                      />
                      <span className="min-w-0 flex-1">
                        <span className="block text-sm font-medium">{student.name}</span>
                        <span className="block text-xs text-muted-foreground">
                          {student.admissionNumber}{student.className ? ` · ${student.className}` : ""}
                        </span>
                      </span>
                    </label>
                  ))
                )}
              </div>

              <div className="mt-4 flex flex-wrap items-center gap-3">
                <Button
                  onClick={saveAssignments}
                  disabled={saving}
                  className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
                >
                  {saving && <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />}
                  Save Student Assignments
                </Button>
                <Button
                  variant="ghost"
                  onClick={() => setSelectedStructure(null)}
                  disabled={saving}
                >
                  Close
                </Button>
                <span className="text-xs text-muted-foreground">
                  {selectedStudentIds.length} selected · changes affect future charge generation only.
                </span>
              </div>
            </>
          ))}
        </section>
      )}
    </div>
  );
}

const inputClass =
  "h-10 w-full rounded-md border bg-background px-3 text-sm";

function Field({
  label,
  children,
}: {
  label: string;
  children:
    React.ReactNode;
}) {
  return (
    <div>
      <label className="mb-2 block text-sm font-medium">
        {label}
      </label>

      {children}
    </div>
  );
}

function Message({
  type,
  children,
}: {
  type: "error" | "success";
  children:
    React.ReactNode;
}) {
  return (
    <div
      className={
        type === "error"
          ? "rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700"
          : "rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-700"
      }
    >
      {children}
    </div>
  );
}

function currency(
  value: number
) {
  return new Intl.NumberFormat(
    "en-NG",
    {
      style: "currency",
      currency: "NGN",
      maximumFractionDigits:
        0,
    }
  ).format(value);
}
