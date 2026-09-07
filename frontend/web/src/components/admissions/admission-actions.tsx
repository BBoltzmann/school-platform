"use client";

import {
  FormEvent,
  useMemo,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  CheckCircle2,
  Clock3,
  LoaderCircle,
  XCircle,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import type {
  AdmissionApplication,
  AdmissionSetup,
} from "@/types/admissions";

type AdmissionActionsProps = {
  application: AdmissionApplication;
  setup: AdmissionSetup;
};

type DecisionType =
  | "waitlist"
  | "reject";

export function AdmissionActions({
  application,
  setup,
}: AdmissionActionsProps) {
  const router = useRouter();

  const [reviewing, setReviewing] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  async function markUnderReview() {
    setReviewing(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/admissions/${application.id}/review`,
        {
          method: "PATCH",
        }
      );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to update application."
        );
        return;
      }

      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setReviewing(false);
    }
  }

  if (
    application.status === "Approved" ||
    application.status === "Rejected"
  ) {
    return null;
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-2">
        {application.status ===
          "Submitted" && (
          <Button
            size="sm"
            variant="outline"
            disabled={reviewing}
            onClick={markUnderReview}
          >
            {reviewing ? (
              <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Clock3 className="mr-2 h-4 w-4" />
            )}

            Review
          </Button>
        )}

        <ApproveAdmissionDialog
          application={application}
          setup={setup}
        />

        <DecisionDialog
          application={application}
          type="waitlist"
        />

        <DecisionDialog
          application={application}
          type="reject"
        />
      </div>

      {error && (
        <p className="text-xs text-red-600">
          {error}
        </p>
      )}
    </div>
  );
}

function DecisionDialog({
  application,
  type,
}: {
  application: AdmissionApplication;
  type: DecisionType;
}) {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [note, setNote] =
    useState("");

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const waitlist =
    type === "waitlist";

  async function submit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setSaving(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/admissions/${application.id}/${type}`,
        {
          method: "POST",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            note:
              note || null,
          }),
        }
      );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            `Unable to ${type} application.`
        );
        return;
      }

      setOpen(false);
      setNote("");
      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={setOpen}
    >
      <DialogTrigger
        render={
          <Button
            size="sm"
            variant="outline"
          >
            {waitlist ? (
              <Clock3 className="mr-2 h-4 w-4" />
            ) : (
              <XCircle className="mr-2 h-4 w-4" />
            )}

            {waitlist
              ? "Waitlist"
              : "Reject"}
          </Button>
        }
      />

      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {waitlist
              ? "Waitlist Application"
              : "Reject Application"}
          </DialogTitle>

          <DialogDescription>
            {waitlist
              ? "Move this applicant to the admission waitlist."
              : "Reject this admission application."}
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={submit}
          className="space-y-5"
        >
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Decision Note
            </label>

            <textarea
              value={note}
              onChange={(event) =>
                setNote(
                  event.target.value
                )
              }
              placeholder={
                waitlist
                  ? "Optional reason for waitlisting..."
                  : "Optional reason for rejection..."
              }
              className="min-h-28 w-full rounded-md border bg-background p-3 text-sm"
            />
          </div>

          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <div className="flex justify-end gap-3">
            <Button
              type="button"
              variant="outline"
              onClick={() =>
                setOpen(false)
              }
            >
              Cancel
            </Button>

            <Button
              type="submit"
              disabled={saving}
            >
              {saving && (
                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
              )}

              Confirm
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function ApproveAdmissionDialog({
  application,
  setup,
}: {
  application: AdmissionApplication;
  setup: AdmissionSetup;
}) {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [admissionNumber, setAdmissionNumber] =
    useState("");

  const [classGroupId, setClassGroupId] =
    useState("");

  const [enrollmentDate, setEnrollmentDate] =
    useState(
      setup.currentSession?.startDate ?? ""
    );

  const [note, setNote] =
    useState("");

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const classes = useMemo(
    () =>
      setup.classes.filter(
        (item) =>
          item.academicLevelId ===
          application.academicLevelId
      ),
    [
      setup.classes,
      application.academicLevelId,
    ]
  );

  function prepare() {
    setError(null);

    if (!classGroupId) {
      setClassGroupId(
        classes[0]?.id ?? ""
      );
    }

    if (
      !enrollmentDate &&
      setup.currentSession
    ) {
      setEnrollmentDate(
        setup.currentSession.startDate
      );
    }
  }

  async function submit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (!classGroupId) {
      setError(
        "Select a class for the student."
      );
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/admissions/${application.id}/approve`,
        {
          method: "POST",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            admissionNumber,
            classGroupId,
            enrollmentDate,
            note:
              note || null,
          }),
        }
      );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to approve application."
        );
        return;
      }

      setOpen(false);
      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setSaving(false);
    }
  }

  const sessionMatches =
    setup.currentSession?.id ===
    application.academicSessionId;

  return (
    <Dialog
      open={open}
      onOpenChange={(value) => {
        setOpen(value);

        if (value) {
          prepare();
        }
      }}
    >
      <DialogTrigger
        render={
          <Button
            size="sm"
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            <CheckCircle2 className="mr-2 h-4 w-4" />
            Approve
          </Button>
        }
      />

      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            Approve Admission
          </DialogTitle>

          <DialogDescription>
            Approval will create this
            applicant as a permanent student
            and enrol them into a class.
          </DialogDescription>
        </DialogHeader>

        {!sessionMatches ? (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
            This application does not belong
            to the current academic session.
          </div>
        ) : (
          <form
            onSubmit={submit}
            className="space-y-5"
          >
            <div className="rounded-lg bg-muted/50 p-4">
              <div className="font-semibold">
                {application.firstName}{" "}
                {application.lastName}
              </div>

              <div className="mt-1 text-sm text-muted-foreground">
                {
                  application.academicLevelName
                }{" "}
                ·{" "}
                {
                  application.academicSessionName
                }
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Admission Number
              </label>

              <Input
                placeholder="e.g. ARC2026902"
                value={admissionNumber}
                onChange={(event) =>
                  setAdmissionNumber(
                    event.target.value
                  )
                }
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Class
              </label>

              <select
                value={classGroupId}
                onChange={(event) =>
                  setClassGroupId(
                    event.target.value
                  )
                }
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                required
              >
                <option value="">
                  Select class
                </option>

                {classes.map(
                  (item) => (
                    <option
                      key={item.id}
                      value={item.id}
                    >
                      {item.name}
                    </option>
                  )
                )}
              </select>

              {classes.length === 0 && (
                <p className="text-xs text-amber-700">
                  No active class exists for
                  this academic level.
                </p>
              )}
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Enrolment Date
              </label>

              <Input
                type="date"
                min={
                  setup.currentSession
                    ?.startDate
                }
                max={
                  setup.currentSession
                    ?.endDate
                }
                value={enrollmentDate}
                onChange={(event) =>
                  setEnrollmentDate(
                    event.target.value
                  )
                }
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Approval Note
              </label>

              <textarea
                value={note}
                onChange={(event) =>
                  setNote(
                    event.target.value
                  )
                }
                placeholder="Optional..."
                className="min-h-20 w-full rounded-md border bg-background p-3 text-sm"
              />
            </div>

            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                {error}
              </div>
            )}

            <div className="flex justify-end gap-3 border-t pt-5">
              <Button
                type="button"
                variant="outline"
                onClick={() =>
                  setOpen(false)
                }
              >
                Cancel
              </Button>

              <Button
                type="submit"
                disabled={
                  saving ||
                  !classGroupId ||
                  classes.length === 0
                }
                className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
              >
                {saving && (
                  <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                )}

                Approve & Create Student
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
