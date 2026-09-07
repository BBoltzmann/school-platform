"use client";

import {
  useState,
} from "react";
import {
  LoaderCircle,
  Plus,
  ReceiptText,
  Users,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type {
  FeeStructure,
  FeesSetup,
} from "@/types/fees";

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

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error
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

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error
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

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error
        );
      }

      setNotice(
        `${result.chargesCreated} charges generated for ${result.studentCount} students.`
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

      <section className="rounded-xl border bg-card">
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
      </section>

      <section className="rounded-xl border bg-card">
        <div className="border-b p-5">
          <h2 className="font-semibold">
            Existing Fee Structures
          </h2>
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
                    onClick={() =>
                      generate(
                        structure
                      )
                    }
                    disabled={saving}
                  >
                    <Users className="mr-2 h-4 w-4" />
                    Generate Student Charges
                  </Button>
                </div>
              )
            )}
          </div>
        )}
      </section>
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
