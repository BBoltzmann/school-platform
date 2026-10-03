"use client";

import {
  useEffect,
  useState,
} from "react";
import {
  LoaderCircle,
  Plus,
  UserRound,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type {
  FeesSetup,
} from "@/types/fees";

type Account = {
  studentId: string;
  admissionNumber: string;
  studentName: string;
  academicTermId: string;
  totalCharges: number;
  appliedPayments: number;
  outstandingBalance: number;
  creditBalance: number;
  charges: {
    id: string;
    feeStructureId: string | null;
    feeStructureLineId: string | null;
    feeStructureName: string | null;
    feeItemName: string;
    description: string;
    amount: number;
    amountPaid: number;
    balance: number;
    isPaid: boolean;
    isRequired: boolean | null;
  }[];
  payments: {
    id: string;
    amount: number;
    allocatedAmount: number;
    creditAmount: number;
    paymentMethod: string;
    receiptNumber: string;
    isReversed: boolean;
    createdAtUtc: string;
  }[];
};

type OptionalComponent = {
  feeStructureId: string;
  feeStructureName: string;
  feeStructureLineId: string;
  feeItemId: string;
  feeItemName: string;
  feeItemCode: string | null;
  templateAmount: number;
  alreadyAdded: boolean;
  existingChargeId: string | null;
  existingAmount: number | null;
};

export function StudentAccountsWorkspace({
  setup,
}: {
  setup: FeesSetup;
}) {
  const [
    studentId,
    setStudentId,
  ] = useState(
    setup.students[0]?.id ??
      ""
  );

  const [
    academicTermId,
    setAcademicTermId,
  ] = useState(
    setup.terms[0]?.id ??
      ""
  );

  const [classFilter, setClassFilter] = useState("");

  const [account, setAccount] =
    useState<Account | null>(
      null
    );

  const [loading, setLoading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null
    );
  const [optionalComponents, setOptionalComponents] = useState<OptionalComponent[]>([]);
  const [optionalLineId, setOptionalLineId] = useState("");
  const [optionalAmount, setOptionalAmount] = useState("");
  const [optionalSaving, setOptionalSaving] = useState(false);
  const [optionalMessage, setOptionalMessage] = useState<string | null>(null);

  async function loadAccount() {
    if (
      !studentId ||
      !academicTermId
    ) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response =
        await fetch(
          `/api/fees/students/${studentId}/account?academicTermId=${academicTermId}`
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error
        );
      }

      setAccount(result);
      await loadOptionalComponents();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to load student account."
      );
    } finally {
      setLoading(false);
    }
  }

  async function loadOptionalComponents() {
    if (!studentId || !academicTermId) return;
    const response = await fetch(`/api/fees/students/${studentId}/optional-components?academicTermId=${academicTermId}`);
    const result = await response.json();
    if (!response.ok) throw new Error(result.error ?? "Unable to load optional components.");
    setOptionalComponents(result);
    const first = result.find((item: OptionalComponent) => !item.alreadyAdded);
    setOptionalLineId(first?.feeStructureLineId ?? "");
    setOptionalAmount(first ? String(first.templateAmount) : "");
  }

  async function addOptionalComponent() {
    const amount = Number(optionalAmount);
    if (!optionalLineId || !Number.isFinite(amount) || amount <= 0) {
      setOptionalMessage("Choose an optional component and enter a valid amount.");
      return;
    }
    setOptionalSaving(true);
    setOptionalMessage(null);
    try {
      const response = await fetch(`/api/fees/students/${studentId}/optional-components`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ academicTermId, feeStructureLineId: optionalLineId, amount }),
      });
      const result = await response.json();
      if (!response.ok) throw new Error(result.error ?? "Unable to add optional component.");
      setOptionalMessage("Optional component added to the account.");
      await loadAccount();
    } catch (exception) {
      setOptionalMessage(exception instanceof Error ? exception.message : "Unable to add optional component.");
    } finally {
      setOptionalSaving(false);
    }
  }

  async function editOptionalCharge(charge: Account["charges"][number]) {
    const entered = window.prompt("Charge amount", String(charge.amount));
    if (entered === null) return;
    const amount = Number(entered);
    if (!Number.isFinite(amount) || amount <= 0 || amount < charge.amountPaid) {
      setOptionalMessage("The amount must be greater than zero and not less than the amount already paid.");
      return;
    }
    const response = await fetch(`/api/fees/students/${studentId}/charges/${charge.id}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ amount }),
    });
    const result = await response.json();
    if (!response.ok) { setOptionalMessage(result.error ?? "Unable to update optional charge."); return; }
    setOptionalMessage("Optional charge updated.");
    await loadAccount();
  }

  const classOptions = Array.from(new Set(
    setup.students
      .map(student => student.className)
      .filter((value): value is string => Boolean(value))
  )).sort();
  const filteredStudents = setup.students.filter(student =>
    !classFilter || student.className === classFilter
  );

  useEffect(() => {
    if (filteredStudents.length > 0 && !filteredStudents.some(student => student.id === studentId)) {
      // Keep the selected account inside the active class filter.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setStudentId(filteredStudents[0].id);
    }
  }, [filteredStudents, studentId]);

  return (
    <div className="space-y-6">
      <section className="rounded-xl border bg-card p-5">
        <div className="grid gap-4 md:grid-cols-[1fr_1fr_1fr_auto] md:items-end">
          <div>
            <label className="mb-2 block text-sm font-medium">
              Student
            </label>

            <select
              value={studentId}
              onChange={event =>
                setStudentId(
                  event.target.value
                )
              }
              className={inputClass}
            >
              {filteredStudents.map(
                student => (
                  <option
                    key={student.id}
                    value={student.id}
                  >
                    {student.name}
                    {" — "}
                    {
                      student.admissionNumber
                    }
                  </option>
                )
              )}
            </select>
          </div>

          <div>
            <label className="mb-2 block text-sm font-medium">Class</label>
            <select
              value={classFilter}
              onChange={event => setClassFilter(event.target.value)}
              className={inputClass}
            >
              <option value="">All classes</option>
              {classOptions.map(option => (
                <option key={option} value={option}>{option}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="mb-2 block text-sm font-medium">
              Term
            </label>

            <select
              value={
                academicTermId
              }
              onChange={event =>
                setAcademicTermId(
                  event.target.value
                )
              }
              className={inputClass}
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
          </div>

          <Button
            onClick={loadAccount}
            disabled={loading}
          >
            {loading && (
              <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
            )}

            View Account
          </Button>
        </div>
      </section>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          {error}
        </div>
      )}

      {account && (
        <>
          <div className="grid gap-4 sm:grid-cols-4">
            <Metric
              label="Total Charges"
              value={money(
                account.totalCharges
              )}
            />

            <Metric
              label="Paid"
              value={money(
                account.appliedPayments
              )}
            />

            <Metric
              label="Outstanding"
              value={money(
                account.outstandingBalance
              )}
            />

            <Metric
              label="Credit"
              value={money(
                account.creditBalance
              )}
            />
          </div>

          <section className="rounded-xl border bg-card p-5">
            <div className="flex items-center justify-between gap-3">
              <div><h2 className="font-semibold">Add Optional Fee Component</h2><p className="mt-1 text-xs text-muted-foreground">Add an assigned optional component with a student-specific amount.</p></div>
              <Plus className="h-5 w-5 text-muted-foreground" />
            </div>
            <div className="mt-4 grid gap-3 md:grid-cols-[1fr_180px_auto] md:items-end">
              <label className="text-sm font-medium">Component<select value={optionalLineId} onChange={event => { const value = event.target.value; setOptionalLineId(value); const item = optionalComponents.find(component => component.feeStructureLineId === value); setOptionalAmount(item ? String(item.templateAmount) : ""); }} className={inputClass}>
                <option value="">Select optional component</option>
                {optionalComponents.map(component => <option key={component.feeStructureLineId} value={component.feeStructureLineId} disabled={component.alreadyAdded}>{component.feeItemName} — {component.feeStructureName}{component.alreadyAdded ? " (already added)" : ""}</option>)}
              </select></label>
              <label className="text-sm font-medium">Charge amount<input type="number" min="0.01" value={optionalAmount} onChange={event => setOptionalAmount(event.target.value)} className={inputClass} /></label>
              <Button onClick={addOptionalComponent} disabled={optionalSaving || !optionalLineId}>{optionalSaving && <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />}Add to Account</Button>
            </div>
            {optionalMessage && <p className="mt-3 text-sm text-muted-foreground">{optionalMessage}</p>}
          </section>

          <section className="overflow-hidden rounded-xl border bg-card">
            <div className="flex items-center gap-3 border-b p-5">
              <UserRound className="h-5 w-5" />

              <div>
                <h2 className="font-semibold">
                  {account.studentName}
                </h2>

                <p className="text-xs text-muted-foreground">
                  {
                    account.admissionNumber
                  }
                </p>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full min-w-[700px] text-sm">
                <thead className="border-b bg-muted/30 text-left">
                  <tr>
                    <th className="px-4 py-3">
                      Charge
                    </th>
                    <th className="px-4 py-3">
                      Fee Structure
                    </th>
                    <th className="px-4 py-3 text-right">
                      Amount
                    </th>
                    <th className="px-4 py-3 text-right">
                      Paid
                    </th>
                    <th className="px-4 py-3 text-right">
                      Balance
                    </th>
                  </tr>
                </thead>

                <tbody className="divide-y">
                  {account.charges.map(
                    charge => (
                      <tr
                        key={
                          charge.id
                        }
                      >
                        <td className="px-4 py-3 font-medium">
                          {
                            charge.description
                          }
                        </td>

                        <td className="px-4 py-3">
                          <span className="rounded-full border px-2 py-1 text-xs">
                            {charge.feeStructureName ??
                              (charge.feeStructureId
                                ? "Legacy / Unlinked"
                                : "Manual charge")}
                            {charge.feeStructureId && <span className="ml-1 text-[10px]">· {charge.isRequired === false ? "Optional" : "Required"}</span>}
                          </span>
                        </td>

                        <td className="px-4 py-3 text-right">
                          {money(
                            charge.amount
                          )}
                        </td>

                        <td className="px-4 py-3 text-right">
                          {money(
                            charge.amountPaid
                          )}
                        </td>

                        <td className="px-4 py-3 text-right font-semibold">
                          {money(
                            charge.balance
                          )}
                          {charge.isRequired === false && charge.amountPaid < charge.amount && <button type="button" onClick={() => void editOptionalCharge(charge)} className="ml-2 text-xs font-medium text-tenant-primary hover:underline">Edit</button>}
                        </td>
                      </tr>
                    )
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </div>
  );
}

const inputClass =
  "h-10 w-full rounded-md border bg-background px-3 text-sm";

function Metric({
  label,
  value,
}: {
  label: string;
  value: string;
}) {
  return (
    <div className="rounded-xl border bg-card p-5">
      <div className="text-xs text-muted-foreground">
        {label}
      </div>

      <div className="mt-2 text-xl font-bold">
        {value}
      </div>
    </div>
  );
}

function money(
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
