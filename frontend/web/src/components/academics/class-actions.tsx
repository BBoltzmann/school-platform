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
import type {
  AcademicLevel,
  Campus,
  ClassGroup,
} from "@/types/academics";

type ClassActionsProps = {
  classGroup: ClassGroup;
  campuses: Campus[];
  levels: AcademicLevel[];
};

export function ClassActions({
  classGroup,
  campuses,
  levels,
}: ClassActionsProps) {
  const router = useRouter();

  const [editOpen, setEditOpen] =
    useState(false);

  const [name, setName] =
    useState(classGroup.name);

  const [campusId, setCampusId] =
    useState(classGroup.campusId);

  const [
    academicLevelId,
    setAcademicLevelId,
  ] = useState(
    classGroup.academicLevelId
  );

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

    setName(classGroup.name);
    setCampusId(classGroup.campusId);
    setAcademicLevelId(
      classGroup.academicLevelId
    );
    setError(null);
  }, [
    editOpen,
    classGroup.name,
    classGroup.campusId,
    classGroup.academicLevelId,
  ]);

  async function updateClass(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setSaving(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/academics/classes/${classGroup.id}`,
        {
          method: "PATCH",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            name,
            campusId,
            academicLevelId,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to update class."
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
        `/api/academics/classes/${classGroup.id}/status`,
        {
          method: "PATCH",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            isActive:
              !classGroup.isActive,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        window.alert(
          result.error ??
            "Unable to change class status."
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
              aria-label={`Actions for ${classGroup.name}`}
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
            Edit Class
          </DropdownMenuItem>

          <DropdownMenuItem
            disabled={changingStatus}
            onClick={changeStatus}
          >
            {classGroup.isActive ? (
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
              Edit Class
            </DialogTitle>

            <DialogDescription>
              Update the class name,
              academic level or campus.
            </DialogDescription>
          </DialogHeader>

          <form
            onSubmit={updateClass}
            className="space-y-5 pt-2"
          >
            <div className="space-y-2">
              <label className="text-sm font-medium">
                Class Name
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
              >
                {levels
                  .filter(
                    (level) =>
                      level.isActive ||
                      level.id ===
                        classGroup.academicLevelId
                  )
                  .map((level) => (
                    <option
                      key={level.id}
                      value={level.id}
                    >
                      {level.name}
                    </option>
                  ))}
              </select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">
                Campus
              </label>

              <select
                value={campusId}
                onChange={(event) =>
                  setCampusId(
                    event.target.value
                  )
                }
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
              >
                {campuses.map(
                  (campus) => (
                    <option
                      key={campus.id}
                      value={campus.id}
                    >
                      {campus.name}
                    </option>
                  )
                )}
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
