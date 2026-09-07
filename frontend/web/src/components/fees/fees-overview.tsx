"use client";

import {
  useEffect,
  useState,
} from "react";
import {
  ArrowDownLeft,
  Banknote,
  CheckCircle2,
  CircleDollarSign,
  CreditCard,
  LoaderCircle,
  Receipt,
  TriangleAlert,
  Users,
  WalletCards,
} from "lucide-react";

import type {
  FeesOverview,
  FeesSetup,
} from "@/types/fees";

export function FeesOverviewWorkspace({
  setup,
}: {
  setup: FeesSetup;
}) {
  const [
    academicTermId,
    setAcademicTermId,
  ] = useState(
    setup.terms[0]?.id ?? ""
  );

  const [
    overview,
    setOverview,
  ] = useState<
    FeesOverview | null
  >(null);

  const [
    loading,
    setLoading,
  ] = useState(false);

  const [
    error,
    setError,
  ] = useState<
    string | null
  >(null);

  useEffect(() => {
    if (!academicTermId) {
      setOverview(null);
      return;
    }

    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);

      try {
        const response =
          await fetch(
            `/api/fees/overview?academicTermId=${encodeURIComponent(
              academicTermId
            )}`
          );

        const result =
          await response.json();

        if (!response.ok) {
          throw new Error(
            result.error ??
              "Unable to load fees overview."
          );
        }

        if (!cancelled) {
          setOverview(result);
        }
      } catch (exception) {
        if (!cancelled) {
          setError(
            exception instanceof Error
              ? exception.message
              : "Unable to load fees overview."
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, [academicTermId]);

  const termName =
    setup.terms.find(
      x =>
        x.id === academicTermId
    )?.name ?? "";

  return (
    <div className="space-y-6">
      <section className="flex flex-col gap-4 rounded-xl border bg-card p-5 shadow-sm sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="font-semibold">
            Fees Overview
          </h2>

          <p className="mt-1 text-xs text-muted-foreground">
            Billing, collections and
            outstanding student
            balances.
          </p>
        </div>

        <div className="w-full sm:w-64">
          <label className="mb-2 block text-xs font-medium text-muted-foreground">
            Academic Term
          </label>

          <select
            value={academicTermId}
            onChange={event =>
              setAcademicTermId(
                event.target.value
              )
            }
            className="h-10 w-full rounded-md border bg-background px-3 text-sm"
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
      </section>

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          {error}
        </div>
      )}

      {loading ? (
        <div className="flex min-h-72 items-center justify-center rounded-xl border bg-card">
          <LoaderCircle className="h-7 w-7 animate-spin text-muted-foreground" />
        </div>
      ) : overview ? (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <MoneyCard
              label="Total Billed"
              amount={
                overview.totalBilled
              }
              icon={
                <Receipt className="h-5 w-5" />
              }
            />

            <MoneyCard
              label="Collected"
              amount={
                overview.totalCollected
              }
              icon={
                <Banknote className="h-5 w-5" />
              }
              hint={`${formatPercent(
                overview.collectionRate
              )} collection rate`}
            />

            <MoneyCard
              label="Outstanding"
              amount={
                overview.totalOutstanding
              }
              icon={
                <TriangleAlert className="h-5 w-5" />
              }
              hint={`${overview.studentsOwing} students owing`}
            />

            <MoneyCard
              label="Student Credit"
              amount={
                overview.creditBalance
              }
              icon={
                <WalletCards className="h-5 w-5" />
              }
              hint="Unallocated overpayment"
            />
          </div>

          <section className="rounded-xl border bg-card p-5 shadow-sm">
            <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
              <div>
                <div className="text-sm font-medium">
                  Collection Progress
                </div>

                <p className="mt-1 text-xs text-muted-foreground">
                  {termName}
                </p>
              </div>

              <div className="grid grid-cols-3 gap-6 text-center">
                <Stat
                  label="Accounts"
                  value={
                    overview.studentAccountCount
                  }
                />

                <Stat
                  label="Fully Paid"
                  value={
                    overview.fullyPaidStudents
                  }
                />

                <Stat
                  label="Owing"
                  value={
                    overview.studentsOwing
                  }
                />
              </div>
            </div>

            <div className="mt-5 h-3 overflow-hidden rounded-full bg-muted">
              <div
                className="h-full rounded-full bg-tenant-primary transition-all"
                style={{
                  width: `${Math.min(
                    100,
                    Math.max(
                      0,
                      overview.collectionRate
                    )
                  )}%`,
                }}
              />
            </div>

            <div className="mt-2 flex justify-between text-xs text-muted-foreground">
              <span>
                {formatPercent(
                  overview.collectionRate
                )} collected
              </span>

              <span>
                {formatCurrency(
                  overview.totalOutstanding
                )}{" "}
                remaining
              </span>
            </div>
          </section>

          <div className="grid gap-6 xl:grid-cols-2">
            <OutstandingTable
              rows={
                overview.topOutstanding
              }
            />

            <RecentPayments
              rows={
                overview.recentPayments
              }
            />
          </div>
        </>
      ) : (
        <div className="rounded-xl border bg-card p-12 text-center">
          <CircleDollarSign className="mx-auto h-10 w-10 text-muted-foreground" />

          <h3 className="mt-3 font-medium">
            No fee data yet
          </h3>

          <p className="mt-1 text-sm text-muted-foreground">
            Create a fee structure
            and generate student
            charges to populate the
            dashboard.
          </p>
        </div>
      )}
    </div>
  );
}

function MoneyCard({
  label,
  amount,
  icon,
  hint,
}: {
  label: string;
  amount: number;
  icon: React.ReactNode;
  hint?: string;
}) {
  return (
    <div className="rounded-xl border bg-card p-5 shadow-sm">
      <div className="flex items-center justify-between">
        <div className="text-sm text-muted-foreground">
          {label}
        </div>

        <div className="rounded-lg bg-muted p-2">
          {icon}
        </div>
      </div>

      <div className="mt-4 text-2xl font-bold tracking-tight">
        {formatCurrency(
          amount
        )}
      </div>

      {hint && (
        <div className="mt-2 text-xs text-muted-foreground">
          {hint}
        </div>
      )}
    </div>
  );
}

function Stat({
  label,
  value,
}: {
  label: string;
  value: number;
}) {
  return (
    <div>
      <div className="text-xl font-bold">
        {value}
      </div>

      <div className="mt-1 text-xs text-muted-foreground">
        {label}
      </div>
    </div>
  );
}

function OutstandingTable({
  rows,
}: {
  rows: FeesOverview["topOutstanding"];
}) {
  return (
    <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className="flex items-start gap-3 border-b p-5">
        <Users className="h-5 w-5" />

        <div>
          <h3 className="font-semibold">
            Top Outstanding Accounts
          </h3>

          <p className="mt-1 text-xs text-muted-foreground">
            Students with the highest
            current balances.
          </p>
        </div>
      </div>

      {rows.length === 0 ? (
        <div className="p-10 text-center">
          <CheckCircle2 className="mx-auto h-8 w-8 text-green-600" />

          <p className="mt-3 text-sm text-muted-foreground">
            No outstanding student
            balances.
          </p>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[600px] text-sm">
            <thead className="border-b bg-muted/30 text-left">
              <tr>
                <th className="px-4 py-3 font-medium">
                  Student
                </th>

                <th className="px-4 py-3 text-right font-medium">
                  Billed
                </th>

                <th className="px-4 py-3 text-right font-medium">
                  Paid
                </th>

                <th className="px-4 py-3 text-right font-medium">
                  Balance
                </th>
              </tr>
            </thead>

            <tbody className="divide-y">
              {rows.map(
                row => (
                  <tr
                    key={
                      row.studentId
                    }
                  >
                    <td className="px-4 py-3">
                      <div className="font-medium">
                        {
                          row.studentName
                        }
                      </div>

                      <div className="mt-1 text-xs text-muted-foreground">
                        {
                          row.admissionNumber
                        }
                      </div>
                    </td>

                    <td className="px-4 py-3 text-right">
                      {formatCurrency(
                        row.totalCharges
                      )}
                    </td>

                    <td className="px-4 py-3 text-right">
                      {formatCurrency(
                        row.totalPaid
                      )}
                    </td>

                    <td className="px-4 py-3 text-right font-semibold text-red-600">
                      {formatCurrency(
                        row.outstandingBalance
                      )}
                    </td>
                  </tr>
                )
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function RecentPayments({
  rows,
}: {
  rows: FeesOverview["recentPayments"];
}) {
  return (
    <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className="flex items-start gap-3 border-b p-5">
        <CreditCard className="h-5 w-5" />

        <div>
          <h3 className="font-semibold">
            Recent Payments
          </h3>

          <p className="mt-1 text-xs text-muted-foreground">
            Latest recorded student
            fee payments.
          </p>
        </div>
      </div>

      {rows.length === 0 ? (
        <div className="p-10 text-center text-sm text-muted-foreground">
          No payments recorded yet.
        </div>
      ) : (
        <div className="divide-y">
          {rows.map(
            row => (
              <div
                key={
                  row.paymentId
                }
                className="flex items-center gap-4 p-4"
              >
                <div className="rounded-full bg-green-50 p-2 text-green-700">
                  <ArrowDownLeft className="h-4 w-4" />
                </div>

                <div className="min-w-0 flex-1">
                  <div className="truncate font-medium">
                    {row.studentName}
                  </div>

                  <div className="mt-1 truncate text-xs text-muted-foreground">
                    {row.paymentMethod}
                    {" · "}
                    {row.receiptNumber}
                  </div>
                </div>

                <div className="text-right">
                  <div className="font-semibold">
                    {formatCurrency(
                      row.amount
                    )}
                  </div>

                  <div className="mt-1 text-xs text-muted-foreground">
                    {formatDate(
                      row.createdAtUtc
                    )}
                  </div>
                </div>
              </div>
            )
          )}
        </div>
      )}
    </section>
  );
}

function formatCurrency(
  value: number
) {
  return new Intl.NumberFormat(
    "en-NG",
    {
      style: "currency",
      currency: "NGN",
      maximumFractionDigits: 0,
    }
  ).format(value);
}

function formatPercent(
  value: number
) {
  return `${new Intl.NumberFormat(
    "en-GB",
    {
      maximumFractionDigits: 1,
    }
  ).format(value)}%`;
}

function formatDate(
  value: string
) {
  return new Intl.DateTimeFormat(
    "en-GB",
    {
      day: "2-digit",
      month: "short",
      year: "numeric",
    }
  ).format(
    new Date(value)
  );
}
