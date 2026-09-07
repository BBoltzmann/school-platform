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

  const [receipt, setReceipt] =
    useState<{
      amount: number;
      allocatedAmount: number;
      creditAmount: number;
      receiptNumber: string;
      paymentMethod: string;
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
