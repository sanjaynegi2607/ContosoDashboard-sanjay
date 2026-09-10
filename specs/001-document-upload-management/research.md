# Research: Document Upload and Management

## Decision: local secure file storage with infrastructure abstraction

The repository is explicitly designed for offline training and local infrastructure. The document feature should therefore use a local storage service outside `wwwroot` and expose it behind `IFileStorageService` so the project can later swap in Azure Blob Storage with minimal business logic changes.

## Rationale

- The project README explicitly recommends local filesystem storage for training scenarios.
- The stakeholder requirements call for secure storage, GUID-based filenames, defended access, and future migration support.
- The application already uses service-layer security and focused dependency injection, which makes file storage abstraction the correct design choice.

## Key implementation decisions

1. Store physical files under a path like `AppData/uploads/{userId}/{projectId or "personal"}/{guid}.{extension}`.
2. Create the unique file path before persisting the metadata record.
3. Validate the extension, content type, and file size before writing.
4. Hide all user-supplied names from the physical storage path to reduce path traversal risk.
5. Keep file metadata in EF Core entities and allow the service layer to enforce project and user access rules.

## Additional design alignment

- The user roles already present in the app cover the required permission model.
- The mock authentication system is sufficient for training and should remain the gating mechanism during implementation.
- Dashboard widgets and notifications can reuse the existing notification service and home page dashboard service.
