# Implementation Plan: Document Upload and Management

**Branch**: `feature-speckit-specify` | **Date**: 2026-09-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/002-document-upload-management/spec.md`

## Summary

Deliver a document-management workflow for authenticated dashboard users with asynchronous malware scanning after upload. The existing Blazor Server application already contains the document entities, SQLite EF Core context, local storage abstraction, document service, dashboard counters, and documents page. The implementation plan completes and verifies the remaining user-facing behavior while preserving service-level authorization, project-member default access, explicit sharing, notifications, and audit logging. A deployment-ready Azure Functions worker will consume Azure Queue Storage messages and update scan status independently of the upload request.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`)  
**Primary Dependencies**: ASP.NET Core Blazor Server, EF Core 10 SQLite, Bootstrap 5, cookie-based mock authentication, Azure Functions isolated worker, Azure Storage Queues  
**Storage**: SQLite database plus local filesystem storage under application `AppData/uploads` outside `wwwroot`  
**Testing**: `dotnet build`; focused manual browser scenarios from [quickstart.md](quickstart.md); add automated service tests where the repository test harness is introduced  
**Target Platform**: Windows ARM64 developer machines and other local .NET 10 environments  
**Project Type**: Single web application  
**Performance Goals**: Upload files up to 25 MB within 30 seconds on a typical local development network; document queries remain filtered by authorization in the database query  
**Constraints**: Offline-capable baseline; Azure integration must be optional/configurable for local training; mock authentication; supported extensions only; files must remain outside the public web root; existing service-oriented layering must remain intact; unscanned or failed-scan files must not be downloadable  
**Scale/Scope**: Training-scale seeded users and projects; one document-management workflow spanning the documents page, project details, dashboard, notifications, EF data model, local storage service, and asynchronous scan worker

## Constitution Check

*GATE: Must pass before Phase 0 research and after Phase 1 design.*

- **Training-First Simplicity**: PASS with a bounded deployment path. The local baseline remains Blazor, EF Core, SQLite, and local storage; Azure Functions and Queue Storage are isolated behind configuration and a small queue contract rather than required for offline training.
- **Security by Default**: PASS. Access is enforced in `IDocumentService`; project members receive default access to project documents, and explicit shares extend access. Files are stored outside `wwwroot` with generated names.
- **Spec-Driven Delivery**: PASS. The work is based on [spec.md](spec.md), with decisions captured in [research.md](research.md), [data-model.md](data-model.md), and [contracts/document-management.md](contracts/document-management.md).
- **Testable, Reviewable Changes**: PASS. The plan defines a build check and focused upload, access, search, sharing, deletion, and audit scenarios in [quickstart.md](quickstart.md).
- **Offline-Compatible Architecture**: PASS with explicit fallback. SQLite, mock authentication, local storage, and a disabled/local queue adapter preserve offline execution; Azure Queue Storage is enabled only in a deployment profile.

**Pre-design gate**: PASS. No unresolved technical unknowns or governance violations.

**Post-design gate**: PASS. The design keeps the existing project structure and introduces no additional project, external service, or constitution exception.

## Project Structure

### Documentation (this feature)

```text
specs/002-document-upload-management/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── document-management.md
└── tasks.md                 # Created later by /speckit.tasks
```

### Source Code

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── Document.cs
│   ├── DocumentShare.cs
│   ├── Project.cs
│   ├── ProjectMember.cs
│   ├── UploadAuditLog.cs
│   └── User.cs
├── Services/
│   ├── DashboardService.cs
│   ├── DocumentService.cs
│   ├── IFileStorageService.cs
│   ├── LocalFileStorageService.cs
│   └── NotificationService.cs
├── Pages/
│   ├── Documents.razor
│   ├── Index.razor
│   └── ProjectDetails.razor
├── Shared/
│   └── NavMenu.razor
├── Program.cs
└── wwwroot/

ContosoDashboard.Scanner/
├── Functions/
│   └── DocumentScanFunction.cs
├── Services/
│   ├── IAntivirusScanner.cs
│   └── AntivirusScanner.cs
└── Program.cs
```

**Structure Decision**: Keep the existing Blazor Server project as the user-facing application and add a narrowly scoped `ContosoDashboard.Scanner` Azure Functions worker for asynchronous scanning. The web app stores a document as `PendingScan`, publishes a queue message after durable metadata/file persistence, and exposes only scan-safe documents. The function validates the message, retrieves the file through the storage boundary, runs the antivirus adapter, and updates scan status through a narrow application contract. Local training uses a disabled scanner or local queue adapter; Azure Queue Storage is the deployment implementation.

## Phase 0: Research

1. Confirm the existing .NET 10, EF Core SQLite, Blazor Server, mock-authentication, and local-storage boundaries.
2. Confirm the access decision: owner access, project-member default access, and explicit share access are all enforced in service queries.
3. Confirm file safety decisions: extension allowlist, 25 MB limit, sanitized display name, GUID storage name, and storage outside `wwwroot`.
4. Define the asynchronous scan lifecycle, Azure Queue Storage message contract, retry/dead-letter behavior, and idempotent status updates.
5. Confirm validation scenarios and document them in [research.md](research.md).

## Phase 1: Design

1. Document entities, relationships, indexes, deletion behavior, and state transitions in [data-model.md](data-model.md).
2. Define service and UI behavior, authorization invariants, and storage expectations in [contracts/document-management.md](contracts/document-management.md).
3. Define the scanner function boundary, queue message schema, and scan-status visibility in [contracts/document-management.md](contracts/document-management.md).
4. Define runnable build, browser, and queue-worker validation flows in [quickstart.md](quickstart.md).
5. Re-run the constitution gate after design artifacts are complete.

## Implementation Notes for Task Generation

- Preserve public service interfaces where possible and extend them only for requirements not covered by the current workflow, such as metadata editing, replacement, sorting, tags, preview, and explicit share listing.
- Keep authorization predicates server-side and reuse them for list, detail, download, and delete operations.
- Ensure failed file persistence does not leave a database record, and ensure failed database persistence cleans up the newly stored file.
- Add scan status fields and a durable queue publication boundary. Set new uploads to `PendingScan`, publish only after metadata and file persistence succeeds, and prevent download/preview until the status is `Clean`.
- Implement the Azure Functions Queue Storage trigger with a stable message containing `DocumentId`, storage object identifier, content hash, and schema version. Make processing idempotent so retries cannot duplicate audit events or regress a terminal result.
- Configure bounded retries and a poison/dead-letter queue. A scan error moves the document to `ScanFailed` with a user-safe message; malware detection moves it to `Quarantined` and blocks access. Never expose scanner internals to the UI.
- Keep the antivirus engine behind `IAntivirusScanner`; use a deterministic local test double or disabled adapter for offline validation and a configured production scanner in the Azure Functions worker.
- Treat `IsDeleted` as a soft-delete visibility state while removing the physical file from local storage according to the feature requirement.
- Update README setup instructions if they still describe .NET 8 or SQL Server LocalDB.
