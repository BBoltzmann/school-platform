"use client";

import {
  FormEvent,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  LoaderCircle,
  Plus,
  UserPlus,
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
import type { StudentSetup } from "@/types/students";

type CreateStudentDialogProps = {
  setup: StudentSetup;
};

export function CreateStudentDialog({
  setup,
}: CreateStudentDialogProps) {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [admissionNumber, setAdmissionNumber] =
    useState("");

  const [firstName, setFirstName] =
    useState("");

  const [middleName, setMiddleName] =
    useState("");

  const [lastName, setLastName] =
    useState("");

  const [dateOfBirth, setDateOfBirth] =
    useState("");

  const [gender, setGender] =
    useState("");

  const [admissionDate, setAdmissionDate] =
    useState("");

  const [email, setEmail] =
    useState("");

  const [phone, setPhone] =
    useState("");

  const [academicLevelId, setAcademicLevelId] =
    useState(
      setup.levels[0]?.id ?? ""
    );

  const [classGroupId, setClassGroupId] =
    useState("");

  const [loading, setLoading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const availableClasses =
    useMemo(
      () =>
        setup.classes.filter(
          (classGroup) =>
            classGroup.academicLevelId ===
            academicLevelId
        ),
      [
        setup.classes,
        academicLevelId,
      ]
    );

  useEffect(() => {
    if (
      setup.levels.length > 0 &&
      !academicLevelId
    ) {
      setAcademicLevelId(
        setup.levels[0].id
      );
    }
  }, [
    setup.levels,
    academicLevelId,
  ]);

  useEffect(() => {
    const classStillValid =
      availableClasses.some(
        (classGroup) =>
          classGroup.id ===
          classGroupId
      );

    if (!classStillValid) {
      setClassGroupId(
        availableClasses[0]?.id ?? ""
      );
    }
  }, [
    availableClasses,
    classGroupId,
  ]);

  function resetForm() {
    setAdmissionNumber("");
    setFirstName("");
    setMiddleName("");
    setLastName("");
    setDateOfBirth("");
    setGender("");
    setAdmissionDate("");
    setEmail("");
    setPhone("");
    setError(null);

    setAcademicLevelId(
      setup.levels[0]?.id ?? ""
    );
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (!setup.currentSession) {
      setError(
        "There is no current academic session."
      );
      return;
    }

    if (!academicLevelId) {
      setError(
        "Select an academic level."
      );
      return;
    }

    if (!classGroupId) {
      setError(
        "Select a class."
      );
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response =
        await fetch(
          "/api/students",
          {
            method: "POST",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify({
              admissionNumber,
              firstName,
              middleName:
                middleName || null,
              lastName,
              dateOfBirth,
              gender,
              admissionDate,
              email:
                email || null,
              phone:
                phone || null,
              academicSessionId:
                setup.currentSession.id,
              academicLevelId,
              classGroupId,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to create student."
        );
        return;
      }

      setOpen(false);
      resetForm();

      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setLoading(false);
    }
  }

  const canCreate =
    setup.currentSession !== null &&
    setup.levels.length > 0 &&
    setup.classes.length > 0;

  return (
    <Dialog
      open={open}
      onOpenChange={(value) => {
        setOpen(value);

        if (!value) {
          setError(null);
        }
      }}
    >
      <DialogTrigger
        render={
          <Button
            disabled={!canCreate}
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            <Plus className="mr-2 h-4 w-4" />
            Add Student
          </Button>
        }
      />

      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <UserPlus className="h-5 w-5" />
            Add Student
          </DialogTitle>

          <DialogDescription>
            Create a permanent student record
            and enrol the student into the
            current academic session.
          </DialogDescription>
        </DialogHeader>

        {!canCreate ? (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
            A current academic session,
            academic level and class are
            required before students can be
            added.
          </div>
        ) : (
          <form
            onSubmit={handleSubmit}
            className="space-y-6 pt-2"
          >
            <div>
              <h3 className="mb-3 text-sm font-semibold">
                Student Information
              </h3>

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <label className="text-sm font-medium">
                    Admission Number
                  </label>

                  <Input
                    placeholder="ARC/2026/001"
                    value={admissionNumber}
                    onChange={(event) =>
                      setAdmissionNumber(
                        event.target.value
                          .toUpperCase()
                      )
                    }
                    required
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium">
                    Admission Date
                  </label>

                  <Input
                    type="date"
                    value={admissionDate}
                    onChange={(event) =>
                      setAdmissionDate(
                        event.target.value
                      )
                    }
                    required
                  />
                </div>

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
                    <option value="">
                      Select gender
                    </option>

                    <option value="Male">
                      Male
                    </option>

                    <option value="Female">
                      Female
                    </option>
                  </select>
                </div>
              </div>
            </div>

            <div className="border-t pt-5">
              <h3 className="mb-3 text-sm font-semibold">
                Contact Information
              </h3>

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <label className="text-sm font-medium">
                    Email
                  </label>

                  <Input
                    type="email"
                    placeholder="Optional"
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
                    placeholder="Optional"
                    value={phone}
                    onChange={(event) =>
                      setPhone(
                        event.target.value
                      )
                    }
                  />
                </div>
              </div>
            </div>

            <div className="border-t pt-5">
              <h3 className="mb-3 text-sm font-semibold">
                Academic Placement
              </h3>

              <div className="mb-4 rounded-lg bg-muted/50 p-3">
                <div className="text-xs text-muted-foreground">
                  Academic Session
                </div>

                <div className="mt-1 font-semibold">
                  {
                    setup.currentSession
                      ?.name
                  }
                </div>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <label className="text-sm font-medium">
                    Academic Level
                  </label>

                  <select
                    value={academicLevelId}
                    onChange={(event) =>
                      setAcademicLevelId(
                        event.target.value
                      )
                    }
                    className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                    required
                  >
                    {setup.levels.map(
                      (level) => (
                        <option
                          key={level.id}
                          value={level.id}
                        >
                          {level.name}
                        </option>
                      )
                    )}
                  </select>
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
                    {availableClasses.length >
                    0 ? (
                      availableClasses.map(
                        (classGroup) => (
                          <option
                            key={
                              classGroup.id
                            }
                            value={
                              classGroup.id
                            }
                          >
                            {
                              classGroup.name
                            }
                          </option>
                        )
                      )
                    ) : (
                      <option value="">
                        No classes available
                      </option>
                    )}
                  </select>
                </div>
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
                disabled={loading}
                className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
              >
                {loading && (
                  <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                )}

                Add Student
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
