"use client";

import {
  ChangeEvent,
  FormEvent,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  CheckCircle2,
  Download,
  File,
  FileCheck2,
  LoaderCircle,
  Trash2,
  Upload,
  XCircle,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type {
  AdmissionDocument,
  AdmissionRequirements,
} from "@/types/admissions";

type AdmissionDocumentsCardProps = {
  applicationId: string;
  documents: AdmissionDocument[];
  requirements: AdmissionRequirements;
};

const documentTypes = [
  "Passport Photograph",
  "Birth Certificate",
  "Previous School Report",
  "Medical Document",
  "Other",
];

const maxFileSize =
  10 * 1024 * 1024;

export function AdmissionDocumentsCard({
  applicationId,
  documents,
  requirements,
}: AdmissionDocumentsCardProps) {
  const router = useRouter();

  const [
    documentType,
    setDocumentType,
  ] = useState(
    "Passport Photograph"
  );

  const [file, setFile] =
    useState<File | null>(null);

  const [uploading, setUploading] =
    useState(false);

  const [
    deletingId,
    setDeletingId,
  ] = useState<string | null>(
    null
  );

  const [error, setError] =
    useState<string | null>(
      null
    );

  function handleFileChange(
    event: ChangeEvent<HTMLInputElement>
  ) {
    const selected =
      event.target.files?.[0] ??
      null;

    setError(null);

    if (
      selected &&
      selected.size >
        maxFileSize
    ) {
      setFile(null);

      setError(
        "The maximum file size is 10 MB."
      );

      event.target.value = "";

      return;
    }

    setFile(selected);
  }

  async function uploadDocument(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (!file) {
      setError(
        "Select a file to upload."
      );

      return;
    }

    setUploading(true);
    setError(null);

    try {
      const formData =
        new FormData();

      formData.append(
        "documentType",
        documentType
      );

      formData.append(
        "file",
        file
      );

      const response =
        await fetch(
          `/api/admissions/${applicationId}/documents`,
          {
            method: "POST",
            body: formData,
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to upload document."
        );

        return;
      }

      setFile(null);

      const input =
        document.getElementById(
          `admission-file-${applicationId}`
        ) as HTMLInputElement | null;

      if (input) {
        input.value = "";
      }

      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setUploading(false);
    }
  }

  async function removeDocument(
    documentId: string
  ) {
    const confirmed =
      window.confirm(
        "Remove this document from the admission application?"
      );

    if (!confirmed) {
      return;
    }

    setDeletingId(
      documentId
    );

    setError(null);

    try {
      const response =
        await fetch(
          `/api/admissions/${applicationId}/documents/${documentId}`,
          {
            method: "DELETE",
          }
        );

      if (!response.ok) {
        let message =
          "Unable to remove document.";

        try {
          const result =
            await response.json();

          message =
            result.error ??
            message;
        } catch {
          // Keep default.
        }

        setError(message);
        return;
      }

      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setDeletingId(null);
    }
  }

  const requirementItems = [
    {
      label:
        "Passport Photograph",
      complete:
        requirements.passportPhotograph,
    },
    {
      label:
        "Birth Certificate",
      complete:
        requirements.birthCertificate,
    },
    {
      label:
        "Previous School Report",
      complete:
        requirements.previousSchoolReport,
    },
    {
      label:
        "Medical Document",
      complete:
        requirements.medicalDocument,
    },
  ];

  return (
    <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className="flex items-center gap-2 border-b px-5 py-4">
        <FileCheck2 className="h-5 w-5" />

        <div>
          <h2 className="font-semibold">
            Documents & Requirements
          </h2>

          <p className="mt-0.5 text-xs text-muted-foreground">
            Supporting documents for
            this admission application.
          </p>
        </div>
      </div>

      <div className="space-y-6 p-5">
        <div>
          <div className="flex items-end justify-between gap-4">
            <div>
              <div className="text-sm font-semibold">
                Admission Requirements
              </div>

              <div className="mt-1 text-xs text-muted-foreground">
                {
                  requirements.completed
                }{" "}
                of{" "}
                {
                  requirements.total
                }{" "}
                requirements complete
              </div>
            </div>

            <div className="text-lg font-bold">
              {
                requirements.percentage
              }
              %
            </div>
          </div>

          <div className="mt-3 h-2 overflow-hidden rounded-full bg-muted">
            <div
              className="h-full rounded-full bg-tenant-primary transition-all"
              style={{
                width: `${requirements.percentage}%`,
              }}
            />
          </div>

          <div className="mt-4 grid gap-2 sm:grid-cols-2">
            {requirementItems.map(
              (item) => (
                <div
                  key={item.label}
                  className="flex items-center gap-2 rounded-lg border p-3"
                >
                  {item.complete ? (
                    <CheckCircle2 className="h-4 w-4 shrink-0 text-green-600" />
                  ) : (
                    <XCircle className="h-4 w-4 shrink-0 text-muted-foreground" />
                  )}

                  <span className="text-sm">
                    {item.label}
                  </span>
                </div>
              )
            )}
          </div>
        </div>

        <div className="border-t pt-5">
          <h3 className="text-sm font-semibold">
            Upload Document
          </h3>

          <form
            onSubmit={
              uploadDocument
            }
            className="mt-4 space-y-4"
          >
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <label className="text-sm font-medium">
                  Document Type
                </label>

                <select
                  value={
                    documentType
                  }
                  onChange={(
                    event
                  ) =>
                    setDocumentType(
                      event.target.value
                    )
                  }
                  className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                >
                  {documentTypes.map(
                    (type) => (
                      <option
                        key={type}
                        value={type}
                      >
                        {type}
                      </option>
                    )
                  )}
                </select>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">
                  File
                </label>

                <input
                  id={`admission-file-${applicationId}`}
                  type="file"
                  accept=".pdf,.jpg,.jpeg,.png,.webp,application/pdf,image/jpeg,image/png,image/webp"
                  onChange={
                    handleFileChange
                  }
                  className="block w-full rounded-md border bg-background p-2 text-sm file:mr-3 file:rounded-md file:border-0 file:bg-muted file:px-3 file:py-1.5 file:text-sm file:font-medium"
                />

                <p className="text-xs text-muted-foreground">
                  PDF, JPEG, PNG or
                  WEBP. Maximum 10 MB.
                </p>
              </div>
            </div>

            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                {error}
              </div>
            )}

            <Button
              type="submit"
              disabled={
                uploading ||
                !file
              }
              className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
            >
              {uploading ? (
                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Upload className="mr-2 h-4 w-4" />
              )}

              Upload Document
            </Button>
          </form>
        </div>

        <div className="border-t pt-5">
          <h3 className="text-sm font-semibold">
            Uploaded Documents
          </h3>

          {documents.length ===
          0 ? (
            <div className="mt-4 rounded-lg border border-dashed p-8 text-center">
              <File className="mx-auto h-8 w-8 text-muted-foreground" />

              <p className="mt-3 text-sm font-medium">
                No documents uploaded
              </p>

              <p className="mt-1 text-xs text-muted-foreground">
                Upload the applicant's
                supporting documents
                above.
              </p>
            </div>
          ) : (
            <div className="mt-4 divide-y rounded-lg border">
              {documents.map(
                (item) => (
                  <div
                    key={item.id}
                    className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between"
                  >
                    <div className="flex min-w-0 items-start gap-3">
                      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-muted">
                        <File className="h-4 w-4" />
                      </div>

                      <div className="min-w-0">
                        <div className="font-medium">
                          {
                            item.documentType
                          }
                        </div>

                        <div className="mt-1 truncate text-xs text-muted-foreground">
                          {
                            item.originalFileName
                          }
                          {" · "}
                          {formatFileSize(
                            item.fileSize
                          )}
                        </div>
                      </div>
                    </div>

                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        nativeButton={
                          false
                        }
                        render={
                          <a
                            href={`/api/admissions/${applicationId}/documents/${item.id}/download`}
                          />
                        }
                      >
                        <Download className="mr-2 h-4 w-4" />
                        Download
                      </Button>

                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        disabled={
                          deletingId ===
                          item.id
                        }
                        onClick={() =>
                          removeDocument(
                            item.id
                          )
                        }
                      >
                        {deletingId ===
                        item.id ? (
                          <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                        ) : (
                          <Trash2 className="mr-2 h-4 w-4" />
                        )}

                        Remove
                      </Button>
                    </div>
                  </div>
                )
              )}
            </div>
          )}
        </div>
      </div>
    </section>
  );
}

function formatFileSize(
  bytes: number
) {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (
    bytes <
    1024 * 1024
  ) {
    return `${(
      bytes / 1024
    ).toFixed(1)} KB`;
  }

  return `${(
    bytes /
    (1024 * 1024)
  ).toFixed(1)} MB`;
}
