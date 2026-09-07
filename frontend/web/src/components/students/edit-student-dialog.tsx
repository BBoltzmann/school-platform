"use client";

import {
  FormEvent,
  useEffect,
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
import type { StudentDetail } from "@/types/students";

type EditStudentDialogProps = {
  student: StudentDetail;
};

export function EditStudentDialog({
  student,
}: EditStudentDialogProps) {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [firstName, setFirstName] =
    useState(student.firstName);

  const [middleName, setMiddleName] =
    useState(student.middleName ?? "");

  const [lastName, setLastName] =
    useState(student.lastName);

  const [dateOfBirth, setDateOfBirth] =
    useState(student.dateOfBirth);

  const [gender, setGender] =
    useState(student.gender);

  const [email, setEmail] =
    useState(student.email ?? "");

  const [phone, setPhone] =
    useState(student.phone ?? "");

  const [status, setStatus] =
    useState(student.status);

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    setFirstName(student.firstName);
    setMiddleName(student.middleName ?? "");
    setLastName(student.lastName);
    setDateOfBirth(student.dateOfBirth);
    setGender(student.gender);
    setEmail(student.email ?? "");
    setPhone(student.phone ?? "");
    setStatus(student.status);
    setError(null);
  }, [open, student]);

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setSaving(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/students/${student.id}`,
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
            dateOfBirth,
            gender,
            email:
              email || null,
            phone:
              phone || null,
            status,
          }),
        }
      );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to update student."
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

  return (
    <Dialog
      open={open}
      onOpenChange={setOpen}
    >
      <DialogTrigger
        render={
          <Button variant="outline">
            <Pencil className="mr-2 h-4 w-4" />
            Edit Student
          </Button>
        }
      />

      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>
            Edit Student
          </DialogTitle>

          <DialogDescription>
            Update the student's personal,
            contact and status information.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit}
          className="space-y-5 pt-2"
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">
                First Name
              </label>

              <Input
                value={firstName}
                onChange={(event) =>
                  setFirstName(
                    event.target.value
                  )
                }
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Middle Name
              </label>

              <Input
                value={middleName}
                onChange={(event) =>
                  setMiddleName(
                    event.target.value
                  )
                }
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Last Name
              </label>

              <Input
                value={lastName}
                onChange={(event) =>
                  setLastName(
                    event.target.value
                  )
                }
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Date of Birth
              </label>

              <Input
                type="date"
                value={dateOfBirth}
                onChange={(event) =>
                  setDateOfBirth(
                    event.target.value
                  )
                }
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Gender
              </label>

              <select
                value={gender}
                onChange={(event) =>
                  setGender(
                    event.target.value
                  )
                }
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                required
              >
                <option value="Male">
                  Male
                </option>

                <option value="Female">
                  Female
                </option>
              </select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Status
              </label>

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

                <option value="Graduated">
                  Graduated
                </option>

                <option value="Withdrawn">
                  Withdrawn
                </option>

                <option value="Suspended">
                  Suspended
                </option>
              </select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Email
              </label>

              <Input
                type="email"
                value={email}
                onChange={(event) =>
                  setEmail(
                    event.target.value
                  )
                }
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Phone
              </label>

              <Input
                value={phone}
                onChange={(event) =>
                  setPhone(
                    event.target.value
                  )
                }
              />
            </div>
          </div>

          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
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
