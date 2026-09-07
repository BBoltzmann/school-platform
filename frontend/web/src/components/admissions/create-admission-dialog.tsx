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
import type { AdmissionSetup } from "@/types/admissions";

type CreateAdmissionDialogProps = {
  setup: AdmissionSetup;
};

function Field({
  label,
  required = false,
  children,
}: {
  label: string;
  required?: boolean;
  children: ReactNode;
}) {
  return (
    <div className="space-y-2">
      <label className="text-sm font-medium">
        {label}

        {required && (
          <span className="ml-1 text-red-500">
            *
          </span>
        )}
      </label>

      {children}
    </div>
  );
}

export function CreateAdmissionDialog({
  setup,
}: CreateAdmissionDialogProps) {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [firstName, setFirstName] =
    useState("");

  const [middleName, setMiddleName] =
    useState("");

  const [lastName, setLastName] =
    useState("");

  const [dateOfBirth, setDateOfBirth] =
    useState("");

  const [gender, setGender] =
    useState("Male");

  const [religion, setReligion] =
    useState("");

  const [
    previousSchoolName,
    setPreviousSchoolName,
  ] = useState("");

  const [
    presentClass,
    setPresentClass,
  ] = useState("");

  const [
    academicLevelId,
    setAcademicLevelId,
  ] = useState(
    setup.levels[0]?.id ?? ""
  );

  const [email, setEmail] =
    useState("");

  const [phone, setPhone] =
    useState("");

  const [
    guardianName,
    setGuardianName,
  ] = useState("");

  const [
    guardianHomeAddress,
    setGuardianHomeAddress,
  ] = useState("");

  const [
    guardianOccupation,
    setGuardianOccupation,
  ] = useState("");

  const [
    guardianPhone,
    setGuardianPhone,
  ] = useState("");

  const [
    guardianOfficeAddress,
    setGuardianOfficeAddress,
  ] = useState("");

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  function resetForm() {
    setFirstName("");
    setMiddleName("");
    setLastName("");
    setDateOfBirth("");
    setGender("Male");
    setReligion("");

    setPreviousSchoolName("");
    setPresentClass("");

    setAcademicLevelId(
      setup.levels[0]?.id ?? ""
    );

    setEmail("");
    setPhone("");

    setGuardianName("");
    setGuardianHomeAddress("");
    setGuardianOccupation("");
    setGuardianPhone("");
    setGuardianOfficeAddress("");

    setError(null);
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (!setup.currentSession) {
      setError(
        "A current academic session is required before applications can be created."
      );

      return;
    }

    setSaving(true);
    setError(null);

    try {
      const response = await fetch(
        "/api/admissions",
        {
          method: "POST",
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
            religion,

            previousSchoolName:
              previousSchoolName || null,

            presentClass:
              presentClass || null,

            email:
              email || null,

            phone:
              phone || null,

            guardianName,
            guardianHomeAddress,
            guardianOccupation,
            guardianPhone,

            guardianOfficeAddress:
              guardianOfficeAddress ||
              null,

            academicSessionId:
              setup.currentSession.id,

            academicLevelId,
          }),
        }
      );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to create admission application."
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
      setSaving(false);
    }
  }

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
          <Button className="bg-tenant-primary text-black hover:bg-tenant-primary/90">
            <Plus className="mr-2 h-4 w-4" />
            New Application
          </Button>
        }
      />

      <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>
            Admission Application
          </DialogTitle>

          <DialogDescription>
            Complete the applicant and
            guardian information below.
          </DialogDescription>
        </DialogHeader>

        {!setup.currentSession ? (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
            Set a current academic session
            before accepting applications.
          </div>
        ) : (
          <form
            onSubmit={handleSubmit}
            className="space-y-8 pt-2"
          >
            <section className="space-y-4">
              <div>
                <h3 className="font-semibold">
                  Applicant Information
                </h3>

                <p className="mt-1 text-xs text-muted-foreground">
                  Personal details of the
                  child applying for admission.
                </p>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <Field
                  label="First Name"
                  required
                >
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

                <Field
                  label="Last Name"
                  required
                >
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

                <Field
                  label="Date of Birth"
                  required
                >
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
                </Field>

                <Field
                  label="Gender"
                  required
                >
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
                </Field>

                <Field
                  label="Religion"
                  required
                >
                  <Input
                    value={religion}
                    onChange={(event) =>
                      setReligion(
                        event.target.value
                      )
                    }
                    placeholder="e.g. Christianity"
                    required
                  />
                </Field>
              </div>
            </section>

            <section className="space-y-4 border-t pt-6">
              <div>
                <h3 className="font-semibold">
                  Previous School Information
                </h3>

                <p className="mt-1 text-xs text-muted-foreground">
                  This may be left blank for
                  children who have not
                  attended school before.
                </p>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Present / Last School">
                  <Input
                    value={
                      previousSchoolName
                    }
                    onChange={(event) =>
                      setPreviousSchoolName(
                        event.target.value
                      )
                    }
                    placeholder="Name of school"
                  />
                </Field>

                <Field label="Present Class">
                  <Input
                    value={presentClass}
                    onChange={(event) =>
                      setPresentClass(
                        event.target.value
                      )
                    }
                    placeholder="e.g. Nursery 2"
                  />
                </Field>
              </div>
            </section>

            <section className="space-y-4 border-t pt-6">
              <div>
                <h3 className="font-semibold">
                  Admission Sought
                </h3>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <Field
                  label="Class Admission Sought"
                  required
                >
                  <select
                    value={
                      academicLevelId
                    }
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
                </Field>

                <Field label="Academic Session">
                  <Input
                    value={
                      setup.currentSession.name
                    }
                    disabled
                  />
                </Field>
              </div>
            </section>

            <section className="space-y-4 border-t pt-6">
              <div>
                <h3 className="font-semibold">
                  Guardian Information
                </h3>

                <p className="mt-1 text-xs text-muted-foreground">
                  Details of the parent or
                  guardian responsible for
                  this applicant.
                </p>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <Field
                  label="Name of Guardian"
                  required
                >
                  <Input
                    value={guardianName}
                    onChange={(event) =>
                      setGuardianName(
                        event.target.value
                      )
                    }
                    required
                  />
                </Field>

                <Field
                  label="Telephone Number"
                  required
                >
                  <Input
                    value={guardianPhone}
                    onChange={(event) =>
                      setGuardianPhone(
                        event.target.value
                      )
                    }
                    required
                  />
                </Field>

                <Field
                  label="Occupation"
                  required
                >
                  <Input
                    value={
                      guardianOccupation
                    }
                    onChange={(event) =>
                      setGuardianOccupation(
                        event.target.value
                      )
                    }
                    required
                  />
                </Field>

                <Field label="Guardian Email">
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
                    <span className="ml-1 text-red-500">
                      *
                    </span>
                  </label>

                  <textarea
                    value={
                      guardianHomeAddress
                    }
                    onChange={(event) =>
                      setGuardianHomeAddress(
                        event.target.value
                      )
                    }
                    className="min-h-24 w-full rounded-md border bg-background p-3 text-sm"
                    required
                  />
                </div>

                <div className="space-y-2 sm:col-span-2">
                  <label className="text-sm font-medium">
                    Office Address
                  </label>

                  <textarea
                    value={
                      guardianOfficeAddress
                    }
                    onChange={(event) =>
                      setGuardianOfficeAddress(
                        event.target.value
                      )
                    }
                    className="min-h-24 w-full rounded-md border bg-background p-3 text-sm"
                    placeholder="Optional"
                  />
                </div>
              </div>
            </section>

            <section className="space-y-4 border-t pt-6">
              <div>
                <h3 className="font-semibold">
                  Applicant Contact
                </h3>
              </div>

              <Field label="Applicant Telephone Number">
                <Input
                  value={phone}
                  onChange={(event) =>
                    setPhone(
                      event.target.value
                    )
                  }
                  placeholder="Optional"
                />
              </Field>
            </section>

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
                disabled={
                  saving ||
                  !academicLevelId
                }
                className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
              >
                {saving && (
                  <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                )}

                Submit Application
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
