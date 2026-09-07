import {
  AlertTriangle,
  ClipboardCheck,
} from "lucide-react";

import { AssessmentWorkspace } from "@/components/assessments/assessment-workspace";
import { getAssessmentSetup } from "@/lib/api/assessments";

export default async function AssessmentsPage() {
  const setup =
    await getAssessmentSetup();

  if (!setup.currentSession) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            Assessments
          </h1>

          <p className="mt-1 text-sm text-muted-foreground">
            Tests, assignments,
            homework, projects,
            examinations and results.
          </p>
        </div>

        <div className="flex gap-3 rounded-xl border border-amber-200 bg-amber-50 p-5 text-amber-800">
          <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" />

          <div>
            <div className="font-semibold">
              Academic session required
            </div>

            <p className="mt-1 text-sm">
              Configure the current
              academic session before
              creating assessments or
              entering scores.
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <ClipboardCheck className="h-6 w-6" />

            <h1 className="text-2xl font-bold tracking-tight">
              Assessments
            </h1>
          </div>

          <p className="mt-1 text-sm text-muted-foreground">
            Create tests, assignments,
            homework, projects and
            examinations, enter class
            scores and calculate
            weighted results.
          </p>
        </div>

        <div className="rounded-lg border bg-card px-4 py-2 text-sm">
          <span className="text-muted-foreground">
            Session:{" "}
          </span>

          <span className="font-medium">
            {setup.currentSession.name}
          </span>
        </div>
      </div>

      <AssessmentWorkspace
        setup={setup}
      />
    </div>
  );
}
