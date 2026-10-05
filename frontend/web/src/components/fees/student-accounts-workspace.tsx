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
  discounts?: number;
  debitAdjustments?: number;
  creditAdjustments?: number;
  unallocatedPaymentCredit?: number;
  ledger?: { type: string; description: string; debit: number; credit: number; occurredAtUtc: string }[];
  discountsApplied?: { id: string; discountApplicationId: string; name: string; amount: number; isReversed: boolean }[];
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
    reference: string | null;
    notes: string | null;
    allocations: {
      studentFeeChargeId: string;
      description: string;
      feeItemName: string;
      feeStructureName: string | null;
      chargeType: string;
      amountAllocated: number;
    }[];
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
  const [manualDescription, setManualDescription] = useState("");
  const [manualFeeItemId, setManualFeeItemId] = useState(setup.feeItems[0]?.id ?? "");
  const [manualAmount, setManualAmount] = useState("");
  const [discounts, setDiscounts] = useState<{ id: string; name: string; defaultAmount: number; isActive: boolean }[]>([]);

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
          result.error ?? result.detail ?? result.title ?? "Unable to load student account."
        );
      }

      setAccount(result);
      await loadDiscounts();
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

  async function loadDiscounts() {
    const response = await fetch("/api/fees/discounts");
    if (response.ok) setDiscounts(await response.json());
  }

  async function applyStudentDiscount() {
    if (!setup.currentSession || discounts.length === 0) { setOptionalMessage("Create an active discount definition first."); return; }
    const selected = window.prompt(`Enter the discount id to apply:\n${discounts.filter(x => x.isActive).map(x => `${x.id} — ${x.name} (${money(x.defaultAmount)})`).join("\n")}`);
    const definition = discounts.find(x => x.id === selected);
    if (!definition) { setOptionalMessage("Choose a valid discount."); return; }
    const amountText = window.prompt("Discount amount", String(definition.defaultAmount));
    if (amountText === null) return;
    const response = await fetch("/api/fees/discounts/apply", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ discountDefinitionId: definition.id, academicSessionId: setup.currentSession.id, academicTermId, audienceType: "SelectedStudents", studentIds: [studentId], amountOverride: Number(amountText), idempotencyKey: `student:${studentId}:${definition.id}:${academicTermId}` }) });
    const result = await response.json();
    if (!response.ok) { setOptionalMessage(result.error ?? "Unable to apply discount."); return; }
    setOptionalMessage("Discount applied to this student.");
    await loadAccount();
  }

  async function addPreviousBalance() {
    if (!setup.currentSession) return;
    const amountText = window.prompt("Previous balance amount (positive)");
    if (amountText === null) return;
    const amount = Number(amountText);
    if (!Number.isFinite(amount) || amount <= 0) { setOptionalMessage("Enter a positive amount."); return; }
    const owes = window.confirm("OK = student owes the school (debit). Cancel = student has a credit.");
    const response = await fetch("/api/fees/adjustments", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ studentId, academicSessionId: setup.currentSession.id, academicTermId, type: owes ? "OpeningDebit" : "OpeningCredit", amount, description: owes ? "Opening debit balance" : "Opening credit balance", reason: window.prompt("Note (optional)") }) });
    const result = await response.json();
    if (!response.ok) { setOptionalMessage(result.error ?? "Unable to add previous balance."); return; }
    setOptionalMessage("Previous balance added.");
    await loadAccount();
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

  async function addManualCharge() {
    const amount = Number(manualAmount);
    if (!manualDescription.trim() || !manualFeeItemId || !Number.isFinite(amount) || amount <= 0) { setOptionalMessage("Enter a description, fee item, and valid amount for the manual charge."); return; }
    const response = await fetch(`/api/fees/students/${studentId}/charges`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ academicTermId, feeItemId: manualFeeItemId, description: manualDescription.trim(), amount }) });
    const result = await response.json();
    if (!response.ok) { setOptionalMessage(result.error ?? "Unable to add manual charge."); return; }
    setManualDescription(""); setManualAmount(""); setOptionalMessage("Manual charge added to the account."); await loadAccount();
  }

  async function removeCharge(charge: Account["charges"][number]) {
    if (!window.confirm(`Remove ${charge.description} ${money(charge.amount)} from ${account?.studentName} — ${account?.admissionNumber}?`)) return;
    const response = await fetch(`/api/fees/students/${studentId}/charges/${charge.id}`, { method: "DELETE" });
    const result = await response.json();
    if (!response.ok) { setOptionalMessage(result.error ?? "Unable to remove charge."); return; }
    setOptionalMessage("Charge removed from the active account.");
    await loadAccount();
  }

  async function voidPayment(payment: Account["payments"][number]) {
    const student = setup.students.find(item => item.id === studentId);
    const term = setup.terms.find(item => item.id === academicTermId);
    const reason = window.prompt(
      `Void ${money(payment.amount)} payment for ${account?.studentName} — ${account?.admissionNumber}${student?.className ? ` — ${student.className}` : ""}?\nTerm: ${term?.name ?? "Selected term"}\nReceipt: ${payment.receiptNumber}\nDate: ${new Date(payment.createdAtUtc).toLocaleDateString()}\nMethod: ${payment.paymentMethod}\nReason (required):`,
    );
    if (reason === null) return;
    if (!reason.trim()) {
      setError("A reason is required to void a payment.");
      return;
    }
    const response = await fetch(`/api/fees/students/${studentId}/payments/${payment.id}/void`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ reason: reason.trim() }),
    });
    const result = await response.json();
    if (!response.ok) {
      setError(result.error ?? "Unable to void payment.");
      return;
    }
    setError(null);
    setOptionalMessage("Payment voided. Outstanding balances have been restored.");
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
            <h2 className="font-semibold">Account breakdown</h2>
            <div className="mt-4 grid gap-3 text-sm sm:grid-cols-2 lg:grid-cols-4">
              <Breakdown label="Discounts" value={money(account.discounts ?? 0)} />
              <Breakdown label="Debit adjustments" value={money(account.debitAdjustments ?? 0)} />
              <Breakdown label="Credit adjustments" value={money(account.creditAdjustments ?? 0)} />
              <Breakdown label="Unallocated payment credit" value={money(account.unallocatedPaymentCredit ?? account.creditBalance)} />
            </div>
            {account.ledger && account.ledger.length > 0 && <div className="mt-5 overflow-x-auto"><table className="w-full text-sm"><thead className="border-b text-left"><tr><th className="py-2">Activity</th><th className="py-2">Description</th><th className="py-2 text-right">Debit</th><th className="py-2 text-right">Credit</th></tr></thead><tbody className="divide-y">{account.ledger.map((entry, index) => <tr key={`${entry.type}-${entry.occurredAtUtc}-${index}`}><td className="py-2 capitalize">{entry.type}</td><td className="py-2">{entry.description}</td><td className="py-2 text-right">{entry.debit ? money(entry.debit) : "—"}</td><td className="py-2 text-right">{entry.credit ? money(entry.credit) : "—"}</td></tr>)}</tbody></table></div>}
            <div className="mt-4 flex flex-wrap gap-2"><Button size="sm" onClick={() => void applyStudentDiscount()}>Add discount</Button><Button size="sm" variant="outline" onClick={() => void addPreviousBalance()}>Add previous balance</Button></div>
            {account.discountsApplied?.filter(x => !x.isReversed).map(discount => <div className="mt-3 flex items-center justify-between rounded border p-2 text-sm" key={discount.id}><span>{discount.name} — {money(discount.amount)}</span><button className="text-red-700 hover:underline" onClick={async () => { const reason = window.prompt("Reason for reversal"); if (!reason) return; const response = await fetch(`/api/fees/discounts/applications/${discount.discountApplicationId}/reverse`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ reason }) }); if (!response.ok) { setOptionalMessage("Unable to reverse discount."); return; } setOptionalMessage("Discount reversed."); await loadAccount(); }}>Reverse discount</button></div>)}
          </section>

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
            <div className="mt-4 border-t pt-4"><div className="text-sm font-medium">Add Manual Charge</div><div className="mt-2 grid gap-3 md:grid-cols-[1fr_180px_180px_auto] md:items-end"><input value={manualDescription} onChange={event => setManualDescription(event.target.value)} placeholder="Description" className={inputClass} /><select value={manualFeeItemId} onChange={event => setManualFeeItemId(event.target.value)} className={inputClass}>{setup.feeItems.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select><input type="number" min="0.01" value={manualAmount} onChange={event => setManualAmount(event.target.value)} placeholder="Amount" className={inputClass} /><Button type="button" variant="outline" onClick={addManualCharge}>Add Charge</Button></div></div>
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
                          <span className="ml-2 rounded-full border px-2 py-1 text-[10px] font-normal">
                            {charge.amountPaid <= 0 ? "Unpaid" : charge.balance <= 0 ? "Paid" : "Partially Paid"}
                          </span>
                            {charge.isRequired !== true && charge.amountPaid < charge.amount && <><button type="button" onClick={() => void editOptionalCharge(charge)} className="ml-2 text-xs font-medium text-tenant-primary hover:underline">Edit</button><button type="button" onClick={() => void removeCharge(charge)} className="ml-2 text-xs font-medium text-red-700 hover:underline">Remove</button></>}
                        </td>
                      </tr>
                    )
                  )}
                </tbody>
              </table>
            </div>
          </section>

          <section className="overflow-hidden rounded-xl border bg-card">
            <div className="border-b p-5">
              <h2 className="font-semibold">Payment History</h2>
              <p className="mt-1 text-xs text-muted-foreground">Recorded payments and their persisted allocations for this term.</p>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[760px] text-sm">
                <thead className="border-b bg-muted/30 text-left">
                  <tr>
                    <th className="px-4 py-3">Date</th>
                    <th className="px-4 py-3">Receipt</th>
                    <th className="px-4 py-3 text-right">Received</th>
                    <th className="px-4 py-3 text-right">Allocated</th>
                    <th className="px-4 py-3 text-right">Credit</th>
                    <th className="px-4 py-3">Method</th>
                    <th className="px-4 py-3">Status</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {account.payments.map(payment => (
                    <tr key={payment.id}>
                      <td className="px-4 py-3">{new Date(payment.createdAtUtc).toLocaleDateString()}</td>
                      <td className="px-4 py-3 font-medium">{payment.receiptNumber}</td>
                      <td className="px-4 py-3 text-right">{money(payment.amount)}</td>
                      <td className="px-4 py-3 text-right">{money(payment.allocatedAmount)}</td>
                      <td className="px-4 py-3 text-right">{money(payment.creditAmount)}</td>
                      <td className="px-4 py-3">{payment.paymentMethod}</td>
                      <td className="px-4 py-3">
                        <span className={`rounded-full border px-2 py-1 text-xs ${payment.isReversed ? "text-red-700" : "text-green-700"}`}>
                          {payment.isReversed ? "Voided" : "Completed"}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right">
                        {!payment.isReversed && <button type="button" onClick={() => void voidPayment(payment)} className="text-xs font-medium text-red-700 hover:underline">Void Payment</button>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {account.payments.map(payment => (
              payment.allocations.length > 0 && (
                <div key={`${payment.id}-allocations`} className="border-t px-5 py-3 text-xs text-muted-foreground">
                  <span className="font-medium text-foreground">{payment.receiptNumber} allocations:</span>{" "}
                  {payment.allocations.map((allocation, index) => (
                    <span key={allocation.studentFeeChargeId}>
                      {index > 0 ? ", " : ""}{allocation.description} ({allocation.chargeType}) {money(allocation.amountAllocated)}
                    </span>
                  ))}
                </div>
              )
            ))}
            {account.payments.length === 0 && <p className="p-5 text-sm text-muted-foreground">No payments recorded for this term.</p>}
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

function Breakdown({ label, value }: { label: string; value: string }) {
  return <div className="rounded-lg bg-muted/40 p-3"><p className="text-muted-foreground">{label}</p><p className="mt-1 font-semibold">{value}</p></div>;
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
