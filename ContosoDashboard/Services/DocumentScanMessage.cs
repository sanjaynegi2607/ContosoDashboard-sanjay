namespace ContosoDashboard.Services;

public sealed record DocumentScanMessage(
    string SchemaVersion,
    int DocumentId,
    string StoredFilePath,
    string ContentHash,
    DateTime UploadedAtUtc);
