"use client";

import {
  useState,
} from "react";
import {
  Banknote,
  LoaderCircle,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type {
  FeesSetup,
} from "@/types/fees";

export function RecordPaymentWorkspace({
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

  const [amount, setAmount] =
    useState("");

  const [
    paymentMethod,
    setPaymentMethod,
  ] = useState(
    "Bank Transfer"
  );

  const [
    reference,
    setReference,
  ] = useState("");

  const [notes, setNotes] =
    useState("");

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null
    );
  const [optionalComponents, setOptionalComponents] = useState<{ feeStructureLineId: string; feeStructureName: string; feeItemName: string; templateAmount: number; alreadyAdded: boolean }[]>([]);
  const [optionalLineId, setOptionalLineId] = useState("");
  const [optionalAmount, setOptionalAmount] = useState("");
  const [optionalMessage, setOptionalMessage] = useState<string | null>(null);

  const [receipt, setReceipt] =
    useState<{
      amount: number;
      allocatedAmount: number;
      creditAmount: number;
      receiptNumber: string;
      paymentMethod: string;
      allocations: {
        studentFeeChargeId: string;
        description: string;
        feeItemName: string;
        feeStructureName: string | null;
        chargeType: string;
        amountAllocated: number;
      }[];
    } | null>(null);

  async function record() {
    setError(null);
    setReceipt(null);

    const numeric =
      Number(amount);

    if (
      !studentId ||
      !academicTermId ||
      !Number.isFinite(
        numeric
      ) ||
      numeric <= 0
    ) {
      setError(
        "Enter a valid payment."
      );
      return;
    }

    setSaving(true);

    try {
      const response =
        await fetch(
          `/api/fees/students/${studentId}/payments`,
          {
            method: "POST",
            headers: {
              "Content-Type":
                "application/json",
            },
            body:
              JSON.stringify({
                academicTermId,
                amount:
                  numeric,
                paymentMethod,
                reference:
                  reference.trim() ||
                  null,
                notes:
                  notes.trim() ||
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

      setReceipt(result);
      setAmount("");
      setReference("");
      setNotes("");
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to record payment."
      );
    } finally {
      setSaving(false);
    }
  }

  async function loadOptionalComponents() {
    if (!studentId || !academicTermId) return;
    const response = await fetch(`/api/fees/students/${studentId}/optional-components?academicTermId=${academicTermId}`);
    const result = await response.json();
    if (!response.ok) throw new Error(result.error ?? "Unable to load optional components.");
    setOptionalComponents(result);
    const first = result.find((item: typeof optionalComponents[number]) => !item.alreadyAdded);
    setOptionalLineId(first?.feeStructureLineId ?? "");
    setOptionalAmount(first ? String(first.templateAmount) : "");
  }

  async function addOptionalComponent() {
    const numeric = Number(optionalAmount);
    if (!optionalLineId || !Number.isFinite(numeric) || numeric <= 0) { setOptionalMessage("Choose an optional component and enter a valid amount."); return; }
    try {
      const response = await fetch(`/api/fees/students/${studentId}/optional-components`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ academicTermId, feeStructureLineId: optionalLineId, amount: numeric }) });
      const result = await response.json();
      if (!response.ok) throw new Error(result.error ?? "Unable to add optional component.");
      setOptionalMessage("Optional component added. It will be included in the next payment allocation.");
      await loadOptionalComponents();
    } catch (exception) { setOptionalMessage(exception instanceof Error ? exception.message : "Unable to add optional component."); }
  }

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_420px]">
      <section className="rounded-xl border bg-card">
        <div className="flex items-start gap-3 border-b p-5">
          <Banknote className="h-5 w-5" />

          <div>
            <h2 className="font-semibold">
              Record Student Payment
            </h2>

            <p className="mt-1 text-xs text-muted-foreground">
              The payment will be
              automatically applied
              to the oldest
              outstanding charges.
            </p>
          </div>
        </div>

        <div className="grid gap-4 p-5 md:grid-cols-2">
          <Field label="Student">
            <select
              value={studentId}
              onChange={event =>
                setStudentId(
                  event.target.value
                )
              }
              className={inputClass}
            >
              {setup.students.map(
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
          </Field>

          <Field label="Term">
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
          </Field>

          <div className="md:col-span-2 rounded-lg border border-dashed p-4">
            <div className="flex flex-wrap items-end gap-3">
              <Field label="Optional component"><select value={optionalLineId} onFocus={() => void loadOptionalComponents()} onChange={event => { setOptionalLineId(event.target.value); const item = optionalComponents.find(component => component.feeStructureLineId === event.target.value); setOptionalAmount(item ? String(item.templateAmount) : ""); }} className={inputClass}><option value="">Add Optional Component</option>{optionalComponents.map(component => <option key={component.feeStructureLineId} value={component.feeStructureLineId} disabled={component.alreadyAdded}>{component.feeItemName} — {component.feeStructureName}{component.alreadyAdded ? " (already added)" : ""}</option>)}</select></Field>
              <Field label="Charge amount"><input type="number" min="0.01" value={optionalAmount} onChange={event => setOptionalAmount(event.target.value)} className={inputClass} /></Field>
              <Button type="button" variant="outline" onClick={addOptionalComponent} disabled={!optionalLineId}>Add Optional Component</Button>
            </div>
            {optionalMessage && <p className="mt-2 text-xs text-muted-foreground">{optionalMessage}</p>}
          </div>

          <Field label="Amount">
            <input
              type="number"
              value={amount}
              onChange={event =>
                setAmount(
                  event.target.value
                )
              }
              className={inputClass}
            />
          </Field>

          <Field label="Payment Method">
            <select
              value={
                paymentMethod
              }
              onChange={event =>
                setPaymentMethod(
                  event.target.value
                )
              }
              className={inputClass}
            >
              <option>
                Bank Transfer
              </option>
              <option>
                Cash
              </option>
              <option>
                POS
              </option>
              <option>
                Card
              </option>
              <option>
                Cheque
              </option>
              <option>
                Other
              </option>
            </select>
          </Field>

          <Field label="Reference">
            <input
              value={reference}
              onChange={event =>
                setReference(
                  event.target.value
                )
              }
              className={inputClass}
            />
          </Field>

          <Field label="Notes">
            <input
              value={notes}
              onChange={event =>
                setNotes(
                  event.target.value
                )
              }
              className={inputClass}
            />
          </Field>

          <div className="md:col-span-2">
            {error && (
              <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                {error}
              </div>
            )}

            <Button
              onClick={record}
              disabled={saving}
              className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
            >
              {saving && (
                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
              )}

              Record Payment
            </Button>
          </div>
        </div>
      </section>

      <section className="rounded-xl border bg-card p-5">
        <h2 className="font-semibold">
          Receipt
        </h2>

        {!receipt ? (
          <p className="mt-4 text-sm text-muted-foreground">
            The receipt will appear
            here after a payment is
            recorded.
          </p>
        ) : (
          <div className="mt-5 space-y-4">
            <ReceiptRow
              label="Receipt No."
              value={
                receipt.receiptNumber
              }
              />

            {receipt.allocations.length > 0 && (
              <div className="border-y py-3">
                <div className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">Allocations</div>
                <div className="space-y-2">
                  {receipt.allocations.map(allocation => (
                    <div key={allocation.studentFeeChargeId} className="flex justify-between gap-4 text-sm">
                      <span>{allocation.description}{allocation.feeStructureName ? ` · ${allocation.feeStructureName}` : ""}<span className="ml-1 text-xs text-muted-foreground">({allocation.chargeType})</span></span>
                      <span className="font-medium">{money(allocation.amountAllocated)}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <ReceiptRow
              label="Amount"
              value={money(
                receipt.amount
              )}
            />

            <ReceiptRow
              label="Applied to Fees"
              value={money(
                receipt.allocatedAmount
              )}
            />

            <ReceiptRow
              label="Credit"
              value={money(
                receipt.creditAmount
              )}
            />

            <ReceiptRow
              label="Method"
              value={
                receipt.paymentMethod
              }
            />
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

function ReceiptRow({
  label,
  value,
}: {
  label: string;
  value: string;
}) {
  return (
    <div className="flex justify-between gap-4 border-b pb-3 text-sm">
      <span className="text-muted-foreground">
        {label}
      </span>

      <span className="font-medium">
        {value}
      </span>
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
