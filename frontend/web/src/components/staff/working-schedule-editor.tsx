"use client";

import type {
  StaffAvailability,
  WorkingDayInput,
} from "@/types/staff";

export const WEEK_DAYS = [
  {
    dayOfWeek: 1,
    label: "Monday",
  },
  {
    dayOfWeek: 2,
    label: "Tuesday",
  },
  {
    dayOfWeek: 3,
    label: "Wednesday",
  },
  {
    dayOfWeek: 4,
    label: "Thursday",
  },
  {
    dayOfWeek: 5,
    label: "Friday",
  },
  {
    dayOfWeek: 6,
    label: "Saturday",
  },
  {
    dayOfWeek: 0,
    label: "Sunday",
  },
];

function normalizeDay(
  value: number | string
) {
  if (typeof value === "number") {
    return value;
  }

  const numeric =
    Number(value);

  if (!Number.isNaN(numeric)) {
    return numeric;
  }

  const names: Record<string, number> = {
    sunday: 0,
    monday: 1,
    tuesday: 2,
    wednesday: 3,
    thursday: 4,
    friday: 5,
    saturday: 6,
  };

  return names[
    value.toLowerCase()
  ] ?? -1;
}

function timeValue(
  value: string | undefined,
  fallback: string
) {
  if (!value) {
    return fallback;
  }

  return value.slice(0, 5);
}

export function createWorkingDays(
  availability: StaffAvailability[] = []
): WorkingDayInput[] {
  return WEEK_DAYS.map(
    (day) => {
      const existing =
        availability.find(
          (item) =>
            normalizeDay(
              item.dayOfWeek
            ) ===
            day.dayOfWeek
        );

      return {
        ...day,
        enabled:
          Boolean(existing),
        startTime:
          timeValue(
            existing?.startTime,
            "08:00"
          ),
        endTime:
          timeValue(
            existing?.endTime,
            "16:00"
          ),
      };
    }
  );
}

export function WorkingScheduleEditor({
  value,
  onChange,
  required = false,
}: {
  value: WorkingDayInput[];
  onChange: (
    value: WorkingDayInput[]
  ) => void;
  required?: boolean;
}) {
  function updateDay(
    dayOfWeek: number,
    changes: Partial<WorkingDayInput>
  ) {
    onChange(
      value.map((day) =>
        day.dayOfWeek ===
        dayOfWeek
          ? {
              ...day,
              ...changes,
            }
          : day
      )
    );
  }

  return (
    <div className="space-y-3">
      <div>
        <div className="text-sm font-semibold">
          Working Schedule
          {required && (
            <span className="ml-1 text-red-500">
              *
            </span>
          )}
        </div>

        <p className="mt-1 text-xs text-muted-foreground">
          Select the days this staff
          member is available and the
          hours they can be scheduled.
        </p>
      </div>

      <div className="overflow-hidden rounded-lg border">
        {value.map((day) => (
          <div
            key={day.dayOfWeek}
            className="grid gap-3 border-b p-4 last:border-b-0 sm:grid-cols-[150px_1fr]"
          >
            <label className="flex items-center gap-3">
              <input
                type="checkbox"
                checked={day.enabled}
                onChange={(event) =>
                  updateDay(
                    day.dayOfWeek,
                    {
                      enabled:
                        event.target
                          .checked,
                    }
                  )
                }
                className="h-4 w-4 accent-yellow-400"
              />

              <span className="text-sm font-medium">
                {day.label}
              </span>
            </label>

            {day.enabled ? (
              <div className="flex flex-wrap items-center gap-2">
                <span className="text-xs text-muted-foreground">
                  From
                </span>

                <input
                  type="time"
                  value={day.startTime}
                  onChange={(event) =>
                    updateDay(
                      day.dayOfWeek,
                      {
                        startTime:
                          event.target
                            .value,
                      }
                    )
                  }
                  className="h-9 rounded-md border bg-background px-3 text-sm"
                  required
                />

                <span className="text-xs text-muted-foreground">
                  To
                </span>

                <input
                  type="time"
                  value={day.endTime}
                  onChange={(event) =>
                    updateDay(
                      day.dayOfWeek,
                      {
                        endTime:
                          event.target
                            .value,
                      }
                    )
                  }
                  className="h-9 rounded-md border bg-background px-3 text-sm"
                  required
                />
              </div>
            ) : (
              <div className="flex h-9 items-center text-xs text-muted-foreground">
                Not working
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
