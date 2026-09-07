namespace SchoolPlatform.Application.Admissions;

public sealed record AdmissionDocumentResult(
    Guid Id,
    string DocumentType,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DateTime UploadedAtUtc);

public sealed record AdmissionRequirementsResult(
    bool PassportPhotograph,
    bool BirthCertificate,
    bool PreviousSchoolReport,
    bool MedicalDocument,
    int Completed,
    int Total,
    int Percentage);
