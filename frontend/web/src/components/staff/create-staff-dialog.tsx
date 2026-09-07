"use client";

import {
  FormEvent,
  ReactNode,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  LoaderCircle,
  Plus,
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

import {
  createWorkingDays,
  WorkingScheduleEditor,
} from "@/components/staff/working-schedule-editor";

import type {
  WorkingDayInput,
} from "@/types/staff";

export function CreateStaffDialog() {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [staffNumber, setStaffNumber] =
    useState("");

  const [firstName, setFirstName] =
    useState("");

  const [middleName, setMiddleName] =
    useState("");

  const [lastName, setLastName] =
    useState("");

  const [gender, setGender] =
    useState("Male");

  const [dateOfBirth, setDateOfBirth] =
    useState("");

  const [email, setEmail] =
    useState("");

  const [phone, setPhone] =
    useState("");

  const [address, setAddress] =
    useState("");

  const [
    employmentDate,
    setEmploymentDate,
  ] = useState("");

  const [jobTitle, setJobTitle] =
    useState("");

  const [department, setDepartment] =
    useState("");

  const [
    employmentType,
    setEmploymentType,
  ] = useState("Full-Time");

  const [
    isTeachingStaff,
    setIsTeachingStaff,
  ] = useState(true);

  const [
    workingDays,
    setWorkingDays,
  ] = useState<
    WorkingDayInput[]
  >(() =>
    createWorkingDays()
  );

  const [saving, setSaving] =
    useState(false);

  const [created, setCreated] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const requiresSchedule =
    employmentType !==
    "Full-Time";

  function reset() {
    setStaffNumber("");
    setFirstName("");
    setMiddleName("");
    setLastName("");
    setGender("Male");
    setDateOfBirth("");
    setEmail("");
    setPhone("");
    setAddress("");
    setEmploymentDate("");
    setJobTitle("");
    setDepartment("");
    setEmploymentType(
      "Full-Time"
    );
    setIsTeachingStaff(true);
    setWorkingDays(
      createWorkingDays()
    );
    setCreated(false);
    setError(null);
  }

  function validateSchedule() {
    if (!requiresSchedule) {
      return null;
    }

    const enabled =
      workingDays.filter(
        (day) => day.enabled
      );

    if (enabled.length === 0) {
      return "Select at least one working day for this staff member.";
    }

    for (const day of enabled) {
      if (
        day.startTime >=
        day.endTime
      ) {
        return `${day.label}: start time must be earlier than end time.`;
      }
    }

    return null;
  }

  async function submit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    const scheduleError =
      validateSchedule();

    if (scheduleError) {
      setError(scheduleError);
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const response =
        await fetch(
          "/api/staff",
          {
            method: "POST",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify({
              staffNumber,
              firstName,
              middleName:
                middleName || null,
              lastName,
              gender,
              dateOfBirth:
                dateOfBirth || null,
              email:
                email || null,
              phone,
              address:
                address || null,
              employmentDate,
              jobTitle,
              department:
                department || null,
              employmentType,
              isTeachingStaff,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to create staff member."
        );
        return;
      }

      if (requiresSchedule) {
        const days =
          workingDays
            .filter(
              (day) =>
                day.enabled
            )
            .map((day) => ({
              dayOfWeek:
                day.dayOfWeek,
              startTime:
                `${day.startTime}:00`,
              endTime:
                `${day.endTime}:00`,
            }));

        const availabilityResponse =
          await fetch(
            `/api/staff/${result.id}/availability`,
            {
              method: "PUT",
              headers: {
                "Content-Type":
                  "application/json",
              },
              body: JSON.stringify({
                days,
              }),
            }
          );

        if (
          !availabilityResponse.ok
        ) {
          let message =
            "Unable to save working schedule.";

          try {
            const payload =
              await availabilityResponse.json();

            message =
              payload.error ??
              message;
          } catch {
            // Keep default.
          }

          setCreated(true);
          router.refresh();

          setError(
            `Staff member was created, but the working schedule could not be saved. ${message}`
          );

          return;
        }
      }

      setOpen(false);
      reset();
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
      onOpenChange={(value) => {
        setOpen(value);

        if (!value) {
          reset();
        }
      }}
    >
      <DialogTrigger
        render={
          <Button className="bg-tenant-primary text-black hover:bg-tenant-primary/90">
            <Plus className="mr-2 h-4 w-4" />
            Add Staff
          </Button>
        }
      />

      <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>
            Add Staff Member
          </DialogTitle>

          <DialogDescription>
            Create a teaching or
            non-teaching staff record.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={submit}
          className="space-y-7"
        >
          <section className="space-y-4">
            <h3 className="font-semibold">
              Personal Information
            </h3>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Staff Number">
                <Input
                  value={staffNumber}
                  onChange={(event) =>
                    setStaffNumber(
                      event.target.value
                    )
                  }
                  placeholder="e.g. ARC-STF-001"
                  required
                />
              </Field>

              <Field label="First Name">
                <Input
                  value={firstName}
                  onChange={(event) =>
                    setFirstName(
                      event.target.value
                    )
                  }
                  required
                />
              </Field>

              <Field label="Middle Name">
                <Input
                  value={middleName}
                  onChange={(event) =>
                    setMiddleName(
                      event.target.value
                    )
                  }
                />
              </Field>

              <Field label="Last Name">
                <Input
                  value={lastName}
                  onChange={(event) =>
                    setLastName(
                      event.target.value
                    )
                  }
                  required
                />
              </Field>

              <Field label="Gender">
                <select
                  value={gender}
                  onChange={(event) =>
                    setGender(
                      event.target.value
                    )
                  }
                  className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                >
                  <option value="Male">
                    Male
                  </option>

                  <option value="Female">
                    Female
                  </option>
                </select>
              </Field>

              <Field label="Date of Birth">
                <Input
                  type="date"
                  value={dateOfBirth}
                  onChange={(event) =>
                    setDateOfBirth(
                      event.target.value
                    )
                  }
                />
              </Field>

              <Field label="Telephone">
                <Input
                  value={phone}
                  onChange={(event) =>
                    setPhone(
                      event.target.value
                    )
                  }
                  required
                />
              </Field>

              <Field label="Email">
                <Input
                  type="email"
                  value={email}
                  onChange={(event) =>
                    setEmail(
                      event.target.value
                    )
                  }
                />
              </Field>

              <div className="space-y-2 sm:col-span-2">
                <label className="text-sm font-medium">
                  Home Address
                </label>

                <textarea
                  value={address}
                  onChange={(event) =>
                    setAddress(
                      event.target.value
                    )
                  }
                  className="min-h-20 w-full rounded-md border bg-background p-3 text-sm"
                />
              </div>
            </div>
          </section>

          <section className="space-y-4 border-t pt-6">
            <h3 className="font-semibold">
              Employment Information
            </h3>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Employment Date">
                <Input
                  type="date"
                  value={employmentDate}
                  onChange={(event) =>
                    setEmploymentDate(
                      event.target.value
                    )
                  }
                  required
                />
              </Field>

              <Field label="Job Title">
                <Input
                  value={jobTitle}
                  onChange={(event) =>
                    setJobTitle(
                      event.target.value
                    )
                  }
                  required
                />
              </Field>

              <Field label="Department">
                <Input
                  value={department}
                  onChange={(event) =>
                    setDepartment(
                      event.target.value
                    )
                  }
                />
              </Field>

              <Field label="Employment Type">
                <select
                  value={employmentType}
                  onChange={(event) =>
                    setEmploymentType(
                      event.target.value
                    )
                  }
                  className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                >
                  <option value="Full-Time">
                    Full-Time
                  </option>

                  <option value="Part-Time">
                    Part-Time
                  </option>

                  <option value="Contract">
                    Contract
                  </option>

                  <option value="Temporary">
                    Temporary
                  </option>
                </select>
              </Field>
            </div>

            <label className="flex items-center gap-3 rounded-lg border p-4">
              <input
                type="checkbox"
                checked={
                  isTeachingStaff
                }
                onChange={(event) =>
                  setIsTeachingStaff(
                    event.target.checked
                  )
                }
                className="h-4 w-4 accent-yellow-400"
              />

              <div>
                <div className="text-sm font-medium">
                  Teaching Staff
                </div>

                <div className="text-xs text-muted-foreground">
                  This staff member can
                  later be assigned
                  subjects and classes.
                </div>
              </div>
            </label>
          </section>

          {requiresSchedule && (
            <section className="border-t pt-6">
              <WorkingScheduleEditor
                value={workingDays}
                onChange={
                  setWorkingDays
                }
                required
              />
            </section>
          )}

          {!requiresSchedule && (
            <div className="rounded-lg border bg-muted/30 p-4 text-sm">
              <div className="font-medium">
                Full-Time Schedule
              </div>

              <p className="mt-1 text-xs text-muted-foreground">
                Full-Time staff will use
                the school's standard
                working schedule when we
                configure timetable hours.
              </p>
            </div>
          )}

          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
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
              Close
            </Button>

            <Button
              type="submit"
              disabled={
                saving ||
                created
              }
              className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
            >
              {saving && (
                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
              )}

              {created
                ? "Staff Created"
                : "Add Staff Member"}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function Field({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <div className="space-y-2">
      <label className="text-sm font-medium">
        {label}
      </label>

      {children}
    </div>
  );
}
