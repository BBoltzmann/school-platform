"use client";

import {
  FormEvent,
  ReactNode,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  LoaderCircle,
  Pencil,
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
  StaffAvailability,
  StaffMember,
  WorkingDayInput,
} from "@/types/staff";

export function EditStaffDialog({
  staff,
  availability,
}: {
  staff: StaffMember;
  availability: StaffAvailability[];
}) {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [firstName, setFirstName] =
    useState(staff.firstName);

  const [middleName, setMiddleName] =
    useState(
      staff.middleName ?? ""
    );

  const [lastName, setLastName] =
    useState(staff.lastName);

  const [gender, setGender] =
    useState(staff.gender);

  const [dateOfBirth, setDateOfBirth] =
    useState(
      staff.dateOfBirth ?? ""
    );

  const [email, setEmail] =
    useState(staff.email ?? "");

  const [phone, setPhone] =
    useState(staff.phone);

  const [address, setAddress] =
    useState(staff.address ?? "");

  const [
    employmentDate,
    setEmploymentDate,
  ] = useState(
    staff.employmentDate
  );

  const [jobTitle, setJobTitle] =
    useState(staff.jobTitle);

  const [department, setDepartment] =
    useState(
      staff.department ?? ""
    );

  const [
    employmentType,
    setEmploymentType,
  ] = useState(
    staff.employmentType
  );

  const [
    isTeachingStaff,
    setIsTeachingStaff,
  ] = useState(
    staff.isTeachingStaff
  );

  const [status, setStatus] =
    useState(staff.status);

  const [
    workingDays,
    setWorkingDays,
  ] = useState<
    WorkingDayInput[]
  >(() =>
    createWorkingDays(
      availability
    )
  );

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null
    );

  const requiresSchedule =
    employmentType !==
    "Full-Time";

  function validateSchedule() {
    if (!requiresSchedule) {
      return null;
    }

    const enabled =
      workingDays.filter(
        (day) => day.enabled
      );

    if (enabled.length === 0) {
      return "Select at least one working day.";
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

  async function saveSchedule() {
    const days =
      requiresSchedule
        ? workingDays
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
            }))
        : [];

    const response =
      await fetch(
        `/api/staff/${staff.id}/availability`,
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

    if (!response.ok) {
      let message =
        "Unable to update working schedule.";

      try {
        const result =
          await response.json();

        message =
          result.error ??
          message;
      } catch {
        // Keep default.
      }

      throw new Error(message);
    }
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
          `/api/staff/${staff.id}`,
          {
            method: "PATCH",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify({
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
              status,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to update staff member."
        );
        return;
      }

      await saveSchedule();

      setOpen(false);
      router.refresh();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to connect to the server."
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
          <Button variant="outline">
            <Pencil className="mr-2 h-4 w-4" />
            Edit Staff
          </Button>
        }
      />

      <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>
            Edit Staff Member
          </DialogTitle>

          <DialogDescription>
            Update personal,
            employment and working
            schedule information.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={submit}
          className="space-y-6"
        >
          <div className="grid gap-4 sm:grid-cols-2">
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

            <Field label="Status">
              <select
                value={status}
                onChange={(event) =>
                  setStatus(
                    event.target.value
                  )
                }
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
              >
                <option value="Active">
                  Active
                </option>

                <option value="Inactive">
                  Inactive
                </option>

                <option value="Suspended">
                  Suspended
                </option>

                <option value="Resigned">
                  Resigned
                </option>

                <option value="Terminated">
                  Terminated
                </option>

                <option value="Retired">
                  Retired
                </option>
              </select>
            </Field>

            <div className="space-y-2 sm:col-span-2">
              <label className="text-sm font-medium">
                Address
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

            <span className="text-sm font-medium">
              Teaching Staff
            </span>
          </label>

          {requiresSchedule ? (
            <section className="border-t pt-6">
              <WorkingScheduleEditor
                value={workingDays}
                onChange={
                  setWorkingDays
                }
                required
              />
            </section>
          ) : (
            <div className="rounded-lg border bg-muted/30 p-4">
              <div className="text-sm font-medium">
                Full-Time Schedule
              </div>

              <p className="mt-1 text-xs text-muted-foreground">
                Saving as Full-Time
                removes any old
                individual availability.
                The school's normal
                timetable hours will
                apply.
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
              Cancel
            </Button>

            <Button
              type="submit"
              disabled={saving}
              className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
            >
              {saving && (
                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
              )}

              Save Changes
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
