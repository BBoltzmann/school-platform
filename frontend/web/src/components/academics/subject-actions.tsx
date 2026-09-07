"use client";

import {
  FormEvent,
  useEffect,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  CircleOff,
  CirclePlay,
  LoaderCircle,
  MoreVertical,
  Pencil,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import type { Subject } from "@/types/academics";

type SubjectActionsProps = {
  subject: Subject;
};

export function SubjectActions({
  subject,
}: SubjectActionsProps) {
  const router = useRouter();

  const [editOpen, setEditOpen] =
    useState(false);

  const [name, setName] =
    useState(subject.name);

  const [code, setCode] =
    useState(subject.code);

  const [category, setCategory] =
    useState(subject.category);

  const [saving, setSaving] =
    useState(false);

  const [
    changingStatus,
    setChangingStatus,
  ] = useState(false);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    if (!editOpen) {
      return;
    }

    setName(subject.name);
    setCode(subject.code);
    setCategory(subject.category);
    setError(null);
  }, [
    editOpen,
    subject.name,
    subject.code,
    subject.category,
  ]);

  async function updateSubject(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setSaving(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/academics/subjects/${subject.id}`,
        {
          method: "PATCH",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            name,
            code,
            category,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to update subject."
        );
        return;
      }

      setEditOpen(false);
      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setSaving(false);
    }
  }

  async function changeStatus() {
    setChangingStatus(true);

    try {
      const response = await fetch(
        `/api/academics/subjects/${subject.id}/status`,
        {
          method: "PATCH",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            isActive: !subject.isActive,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        window.alert(
          result.error ??
            "Unable to change subject status."
        );
        return;
      }

      router.refresh();
    } catch {
      window.alert(
        "Unable to connect to the server."
      );
    } finally {
      setChangingStatus(false);
    }
  }

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger
          render={
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              aria-label={`Actions for ${subject.name}`}
            >
              <MoreVertical className="h-4 w-4" />
            </Button>
          }
        />

        <DropdownMenuContent align="end">
          <DropdownMenuItem
            onClick={() =>
              setEditOpen(true)
            }
          >
            <Pencil className="mr-2 h-4 w-4" />
            Edit Subject
          </DropdownMenuItem>

          <DropdownMenuItem
            disabled={changingStatus}
            onClick={changeStatus}
          >
            {subject.isActive ? (
              <>
                <CircleOff className="mr-2 h-4 w-4" />
                Deactivate
              </>
            ) : (
              <>
                <CirclePlay className="mr-2 h-4 w-4" />
                Activate
              </>
            )}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <Dialog
        open={editOpen}
        onOpenChange={setEditOpen}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              Edit Subject
            </DialogTitle>

            <DialogDescription>
              Update the subject name, code
              or category.
            </DialogDescription>
          </DialogHeader>

          <form
            onSubmit={updateSubject}
            className="space-y-5 pt-2"
          >
            <div className="space-y-2">
              <label className="text-sm font-medium">
                Subject Name
              </label>

              <Input
                value={name}
                onChange={(event) =>
                  setName(event.target.value)
                }
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Subject Code
              </label>

              <Input
                value={code}
                onChange={(event) =>
                  setCode(
                    event.target.value.toUpperCase()
                  )
                }
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Category
              </label>

              <select
                value={category}
                onChange={(event) =>
                  setCategory(
                    event.target.value
                  )
                }
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
              >
                <option value="Core">
                  Core
                </option>

                <option value="Elective">
                  Elective
                </option>

                <option value="Vocational">
                  Vocational
                </option>

                <option value="Co-Curricular">
                  Co-Curricular
                </option>
              </select>
            </div>

            {error && (
              <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                {error}
              </div>
            )}

            <div className="flex justify-end gap-3">
              <Button
                type="button"
                variant="outline"
                onClick={() =>
                  setEditOpen(false)
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
    </>
  );
}
