# Research: Document Upload and Management

## Decision: Preserve the existing single-project Blazor architecture

- **Rationale**: The repository already separates models, EF data access, services, and Razor pages. The feature can be completed within those boundaries and remains understandable for training.
- **Alternatives considered**: A separate REST API or frontend project was rejected because the application is a small offline Blazor Server training project and the constitution requires simplicity.

## Decision: Use EF Core SQLite for document metadata

- **Rationale**: SQLite is the repository's current portable local database and works on the user's ARM64 Windows environment. Existing `ApplicationDbContext` relationships and indexes already provide the needed metadata boundary.
- **Alternatives considered**: SQL Server LocalDB was rejected because it is not appropriate for the target ARM64/offline environment; a file-only catalog was rejected because sharing, authorization, notifications, and audit records require relational data.

## Decision: Enforce access in the document service

- **Rationale**: `DocumentService` is the nearest existing ownership boundary for list, detail, download, share, and delete behavior. Every query must combine owner access, project-member access for attached documents, and active explicit shares before returning data.
- **Alternatives considered**: UI-only hiding was rejected because it does not protect direct object access; global role checks alone were rejected because document visibility depends on ownership and project membership.

## Decision: Use allowlisted extensions and a 25 MB server-side limit

- **Rationale**: The stakeholder requirements specify supported document and image formats and a 25 MB maximum. The existing service already validates extensions and stream length; the UI must surface those failures clearly.
- **Alternatives considered**: Trusting browser MIME types was rejected because MIME values are client-controlled and may be missing or malformed. Blocking all duplicate display names was rejected because generated storage names safely permit same-name files with different content.

## Decision: Keep physical files outside `wwwroot` behind `IFileStorageService`

- **Rationale**: `LocalFileStorageService` already generates GUID-based file names and stores files under application `AppData/uploads`. The abstraction preserves a future cloud-storage replacement without coupling business logic to the filesystem.
- **Alternatives considered**: Public static files were rejected because direct URLs would bypass authorization; storing binary content in SQLite was rejected because it complicates the training data model and local file replacement.

## Decision: Use soft-delete metadata with physical file deletion

- **Rationale**: `IsDeleted` preserves audit references while the physical file is removed after an authorized delete. Queries exclude deleted records, and audit logs remain available.
- **Alternatives considered**: Hard-deleting the document row was rejected because audit records need a stable historical reference; retaining the file was rejected because deleted content should not remain accessible.

## Decision: Validate through build plus focused browser scenarios

- **Rationale**: The repository currently has no test project. `dotnet build` is the available executable check, while the feature's security and upload flows are best demonstrated through the seeded mock-auth users and scenarios in `quickstart.md`.
- **Alternatives considered**: Claiming automated unit coverage without a test harness was rejected. Adding a broad test framework is deferred to task generation unless implementation scope requires it.

## Decision: Process virus scans asynchronously with Azure Functions and Queue Storage

- **Rationale**: Upload requests should finish after durable file and metadata persistence, while scanning can be slow or unavailable. An Azure Functions Queue Storage trigger provides retryable background processing, isolates antivirus dependencies from the Blazor application, and supports scale-out without holding a Blazor circuit open.
- **Alternatives considered**: Scanning inline during the upload request was rejected because it increases latency and couples request availability to the scanner. A hosted background thread in the web process was rejected because it is not durable across restarts and does not provide queue retry/dead-letter behavior.

## Decision: Use a pending-to-terminal scan state machine

- **Rationale**: New documents begin as `PendingScan` and become `Clean`, `Quarantined`, or `ScanFailed`. Downloads and previews are allowed only for `Clean` documents. The queue message includes a schema version, document ID, storage identifier, and content hash so retries are idempotent and stale messages can be rejected.
- **Alternatives considered**: Treating an uploaded file as immediately trusted was rejected because it creates a window for unauthorized malware access. Deleting every file after a transient scanner failure was rejected because retryable infrastructure failures should not destroy user data.

## Decision: Keep Azure scanning optional for offline training

- **Rationale**: The constitution requires offline compatibility. The application therefore uses configuration to select a local disabled/test scanner and local queue adapter during training, while the Azure Functions and Queue Storage implementation is the deployment-ready path.
- **Alternatives considered**: Requiring Azure resources for every developer was rejected because it conflicts with the repository's training-first and offline principles.
