"use client";

import {
  FormEvent,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  LoaderCircle,
  Plus,
  Search,
  UserRound,
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
import type { Guardian } from "@/types/guardians";

type AddGuardianDialogProps = {
  studentId: string;
};

type Mode =
  | "create"
  | "existing";

export function AddGuardianDialog({
  studentId,
}: AddGuardianDialogProps) {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [mode, setMode] =
    useState<Mode>("create");

  const [firstName, setFirstName] =
    useState("");

  const [middleName, setMiddleName] =
    useState("");

  const [lastName, setLastName] =
    useState("");

  const [email, setEmail] =
    useState("");

  const [phone, setPhone] =
    useState("");

  const [
    alternatePhone,
    setAlternatePhone,
  ] = useState("");

  const [occupation, setOccupation] =
    useState("");

  const [address, setAddress] =
    useState("");

  const [
    relationship,
    setRelationship,
  ] = useState("Mother");

  const [
    isPrimaryContact,
    setIsPrimaryContact,
  ] = useState(false);

  const [
    isEmergencyContact,
    setIsEmergencyContact,
  ] = useState(false);

  const [
    canPickUpStudent,
    setCanPickUpStudent,
  ] = useState(true);

  const [
    livesWithStudent,
    setLivesWithStudent,
  ] = useState(false);

  const [search, setSearch] =
    useState("");

  const [
    searchResults,
    setSearchResults,
  ] = useState<Guardian[]>([]);

  const [
    selectedGuardianId,
    setSelectedGuardianId,
  ] = useState("");

  const [searching, setSearching] =
    useState(false);

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  function resetForm() {
    setFirstName("");
    setMiddleName("");
    setLastName("");
    setEmail("");
    setPhone("");
    setAlternatePhone("");
    setOccupation("");
    setAddress("");

    setRelationship("Mother");
    setIsPrimaryContact(false);
    setIsEmergencyContact(false);
    setCanPickUpStudent(true);
    setLivesWithStudent(false);

    setSearch("");
    setSearchResults([]);
    setSelectedGuardianId("");

    setError(null);
  }

  async function searchExistingGuardians() {
    if (!search.trim()) {
      setError(
        "Enter a guardian name, phone number or email."
      );

      return;
    }

    setSearching(true);
    setError(null);

    try {
      const response =
        await fetch(
          `/api/guardians?search=${encodeURIComponent(search)}`
        );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to search guardians."
        );

        return;
      }

      setSearchResults(result);

      if (result.length === 0) {
        setSelectedGuardianId("");
      }
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setSearching(false);
    }
  }

  async function createGuardian(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setSaving(true);
    setError(null);

    try {
      const response =
        await fetch(
          `/api/students/${studentId}/guardians`,
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
              email:
                email || null,
              phone,
              alternatePhone:
                alternatePhone || null,
              occupation:
                occupation || null,
              address:
                address || null,
              relationship,
              isPrimaryContact,
              isEmergencyContact,
              canPickUpStudent,
              livesWithStudent,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to add guardian."
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

  async function linkExistingGuardian(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (!selectedGuardianId) {
      setError(
        "Select an existing guardian."
      );

      return;
    }

    setSaving(true);
    setError(null);

    try {
      const response =
        await fetch(
          `/api/students/${studentId}/guardians/link`,
          {
            method: "POST",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify({
              guardianId:
                selectedGuardianId,
              relationship,
              isPrimaryContact,
              isEmergencyContact,
              canPickUpStudent,
              livesWithStudent,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to link guardian."
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

  function RelationshipFields() {
    return (
      <>
        <div className="space-y-2">
          <label className="text-sm font-medium">
            Relationship to Student
          </label>

          <select
            value={relationship}
            onChange={(event) =>
              setRelationship(
                event.target.value
              )
            }
            className="h-10 w-full rounded-md border bg-background px-3 text-sm"
          >
            <option value="Mother">
              Mother
            </option>

            <option value="Father">
              Father
            </option>

            <option value="Guardian">
              Guardian
            </option>

            <option value="Grandmother">
              Grandmother
            </option>

            <option value="Grandfather">
              Grandfather
            </option>

            <option value="Aunt">
              Aunt
            </option>

            <option value="Uncle">
              Uncle
            </option>

            <option value="Sibling">
              Sibling
            </option>

            <option value="Other">
              Other
            </option>
          </select>
        </div>

        <div className="grid gap-3 sm:grid-cols-2">
          <label className="flex items-center gap-3 rounded-lg border p-3">
            <input
              type="checkbox"
              checked={isPrimaryContact}
              onChange={(event) =>
                setIsPrimaryContact(
                  event.target.checked
                )
              }
              className="h-4 w-4 accent-yellow-400"
            />

            <span className="text-sm">
              Primary Contact
            </span>
          </label>

          <label className="flex items-center gap-3 rounded-lg border p-3">
            <input
              type="checkbox"
              checked={isEmergencyContact}
              onChange={(event) =>
                setIsEmergencyContact(
                  event.target.checked
                )
              }
              className="h-4 w-4 accent-yellow-400"
            />

            <span className="text-sm">
              Emergency Contact
            </span>
          </label>

          <label className="flex items-center gap-3 rounded-lg border p-3">
            <input
              type="checkbox"
              checked={canPickUpStudent}
              onChange={(event) =>
                setCanPickUpStudent(
                  event.target.checked
                )
              }
              className="h-4 w-4 accent-yellow-400"
            />

            <span className="text-sm">
              Can Pick Up Student
            </span>
          </label>

          <label className="flex items-center gap-3 rounded-lg border p-3">
            <input
              type="checkbox"
              checked={livesWithStudent}
              onChange={(event) =>
                setLivesWithStudent(
                  event.target.checked
                )
              }
              className="h-4 w-4 accent-yellow-400"
            />

            <span className="text-sm">
              Lives With Student
            </span>
          </label>
        </div>
      </>
    );
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
          <Button
            size="sm"
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            <Plus className="mr-2 h-4 w-4" />
            Add Guardian
          </Button>
        }
      />

      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            Add Guardian
          </DialogTitle>

          <DialogDescription>
            Create a new guardian or link an
            existing guardian to this
            student.
          </DialogDescription>
        </DialogHeader>

        <div className="grid grid-cols-2 rounded-lg bg-muted p-1">
          <button
            type="button"
            onClick={() => {
              setMode("create");
              setError(null);
            }}
            className={
              mode === "create"
                ? "rounded-md bg-background px-3 py-2 text-sm font-medium shadow-sm"
                : "rounded-md px-3 py-2 text-sm text-muted-foreground"
            }
          >
            Create New
          </button>

          <button
            type="button"
            onClick={() => {
              setMode("existing");
              setError(null);
            }}
            className={
              mode === "existing"
                ? "rounded-md bg-background px-3 py-2 text-sm font-medium shadow-sm"
                : "rounded-md px-3 py-2 text-sm text-muted-foreground"
            }
          >
            Link Existing
          </button>
        </div>

        {mode === "create" ? (
          <form
            onSubmit={createGuardian}
            className="space-y-5"
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
                  Phone
                </label>

                <Input
                  value={phone}
                  onChange={(event) =>
                    setPhone(
                      event.target.value
                    )
                  }
                  required
                />
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
                  Alternate Phone
                </label>

                <Input
                  value={alternatePhone}
                  onChange={(event) =>
                    setAlternatePhone(
                      event.target.value
                    )
                  }
                />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">
                  Occupation
                </label>

                <Input
                  value={occupation}
                  onChange={(event) =>
                    setOccupation(
                      event.target.value
                    )
                  }
                />
              </div>

              <div className="space-y-2 sm:col-span-2">
                <label className="text-sm font-medium">
                  Address
                </label>

                <Input
                  value={address}
                  onChange={(event) =>
                    setAddress(
                      event.target.value
                    )
                  }
                />
              </div>
            </div>

            <RelationshipFields />

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

                Add Guardian
              </Button>
            </div>
          </form>
        ) : (
          <form
            onSubmit={linkExistingGuardian}
            className="space-y-5"
          >
            <div className="space-y-2">
              <label className="text-sm font-medium">
                Search Existing Guardians
              </label>

              <div className="flex gap-2">
                <Input
                  placeholder="Name, phone or email"
                  value={search}
                  onChange={(event) =>
                    setSearch(
                      event.target.value
                    )
                  }
                />

                <Button
                  type="button"
                  variant="outline"
                  disabled={searching}
                  onClick={
                    searchExistingGuardians
                  }
                >
                  {searching ? (
                    <LoaderCircle className="h-4 w-4 animate-spin" />
                  ) : (
                    <Search className="h-4 w-4" />
                  )}
                </Button>
              </div>
            </div>

            {searchResults.length > 0 && (
              <div className="space-y-2">
                {searchResults.map(
                  (guardian) => (
                    <button
                      key={guardian.id}
                      type="button"
                      onClick={() =>
                        setSelectedGuardianId(
                          guardian.id
                        )
                      }
                      className={
                        selectedGuardianId ===
                        guardian.id
                          ? "flex w-full items-center gap-3 rounded-lg border-2 border-yellow-400 bg-yellow-50 p-3 text-left"
                          : "flex w-full items-center gap-3 rounded-lg border p-3 text-left hover:bg-muted/50"
                      }
                    >
                      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-muted">
                        <UserRound className="h-5 w-5" />
                      </div>

                      <div>
                        <div className="font-medium">
                          {guardian.firstName}{" "}
                          {guardian.lastName}
                        </div>

                        <div className="text-xs text-muted-foreground">
                          {guardian.phone}
                          {guardian.email
                            ? ` · ${guardian.email}`
                            : ""}
                        </div>
                      </div>
                    </button>
                  )
                )}
              </div>
            )}

            <RelationshipFields />

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
                disabled={
                  saving ||
                  !selectedGuardianId
                }
                className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
              >
                {saving && (
                  <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                )}

                Link Guardian
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
