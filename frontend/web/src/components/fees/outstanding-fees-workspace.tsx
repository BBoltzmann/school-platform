"use client";

import {
  useEffect,
  useState,
} from "react";
import {
  LoaderCircle,
  TriangleAlert,
} from "lucide-react";

import type {
  FeesSetup,
  OutstandingStudent,
} from "@/types/fees";

export function OutstandingFeesWorkspace({
  setup,
}: {
  setup: FeesSetup;
}) {
  const [
    academicTermId,
    setAcademicTermId,
  ] = useState(
    setup.terms[0]?.id ??
      ""
  );

  const [rows, setRows] =
    useState<
      OutstandingStudent[]
    >([]);

  const [loading, setLoading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null
    );

  useEffect(() => {
    if (!academicTermId) {
      return;
    }

    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [academicTermId]);

  async function load() {
    setLoading(true);
    setError(null);

    try {
      const response =
        await fetch(
          `/api/fees/outstanding?academicTermId=${academicTermId}`
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error
        );
      }

      setRows(result);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to load outstanding fees."
      );
    } finally {
      setLoading(false);
    }
  }

  const total =
    rows.reduce(
      (sum, row) =>
        sum +
        row.outstandingBalance,
      0
    );

  return (
    <section className="overflow-hidden rounded-xl border bg-card">
      <div className="flex flex-col gap-4 border-b p-5 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <TriangleAlert className="h-5 w-5" />

            <h2 className="font-semibold">
              Outstanding Fees
            </h2>
          </div>

          <p className="mt-1 text-xs text-muted-foreground">
            Students with unpaid
            balances.
          </p>
        </div>

        <select
          value={
            academicTermId
          }
          onChange={event =>
            setAcademicTermId(
              event.target.value
            )
          }
          className="h-10 rounded-md border bg-background px-3 text-sm"
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

      <div className="border-b bg-muted/20 p-5">
        <div className="text-xs text-muted-foreground">
          Total Outstanding
        </div>

        <div className="mt-2 text-2xl font-bold">
          {money(total)}
        </div>
      </div>

      {error && (
        <div className="p-5 text-sm text-red-600">
          {error}
        </div>
      )}

      {loading ? (
        <div className="flex justify-center p-12">
          <LoaderCircle className="h-6 w-6 animate-spin" />
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[750px] text-sm">
            <thead className="border-b bg-muted/30 text-left">
              <tr>
                <th className="px-5 py-3">
                  Student
                </th>
                <th className="px-5 py-3 text-right">
                  Billed
                </th>
                <th className="px-5 py-3 text-right">
                  Paid
                </th>
                <th className="px-5 py-3 text-right">
                  Outstanding
                </th>
              </tr>
            </thead>

            <tbody className="divide-y">
              {rows.map(row => (
                <tr
                  key={row.studentId}
                >
                  <td className="px-5 py-4">
                    <div className="font-medium">
                      {row.studentName}
                    </div>

                    <div className="text-xs text-muted-foreground">
                      {
                        row.admissionNumber
                      }
                    </div>
                  </td>

                  <td className="px-5 py-4 text-right">
                    {money(
                      row.totalCharges
                    )}
                  </td>

                  <td className="px-5 py-4 text-right">
                    {money(
                      row.totalPaid
                    )}
                  </td>

                  <td className="px-5 py-4 text-right font-semibold text-red-600">
                    {money(
                      row.outstandingBalance
                    )}
                  </td>
                </tr>
              ))}

              {rows.length === 0 && (
                <tr>
                  <td
                    colSpan={4}
                    className="p-10 text-center text-muted-foreground"
                  >
                    No outstanding
                    balances.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
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
