"use client";

import {
  useState,
} from "react";
import {
  LoaderCircle,
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
    feeItemName: string;
    description: string;
    amount: number;
    amountPaid: number;
    balance: number;
    isPaid: boolean;
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

  return (
    <div className="space-y-6">
      <section className="rounded-xl border bg-card p-5">
        <div className="grid gap-4 md:grid-cols-[1fr_1fr_auto] md:items-end">
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
