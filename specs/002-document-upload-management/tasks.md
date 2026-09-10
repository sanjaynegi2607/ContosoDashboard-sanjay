---

description: "Executable task list for Document Upload and Management"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `specs/002-document-upload-management/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/document-management.md`
**Project**: .NET 10 Blazor Server application with an Azure Functions scan worker

## Phase 1: Setup

**Purpose**: Establish the source-project and deployment configuration needed by the feature.

- [ ] T001 Create the `ContosoDashboard.Scanner` Azure Functions isolated-worker project structure with `ContosoDashboard.Scanner/ContosoDashboard.Scanner.csproj` and `ContosoDashboard.Scanner/Program.cs` targeting the repository's .NET 10 toolchain.
- [ ] T002 [P] Add Azure Functions Queue Storage and worker configuration placeholders in `ContosoDashboard.Scanner/host.json`, `ContosoDashboard.Scanner/local.settings.json.example`, and `ContosoDashboard/appsettings.json` without committing secrets.
- [ ] T003 [P] Update `ContosoDashboard/ContosoDashboard.csproj` with the minimal Azure Storage Queue client dependency required for publishing scan messages, preserving existing EF Core SQLite packages.
- [ ] T004 [P] Update `README.md` with the .NET 10, SQLite, offline scanner fallback, and optional Azure Queue Storage/Azure Functions setup paths.

---

## Phase 2: Foundational

**Purpose**: Implement blocking data, storage, queue, and security foundations required by every user story.

- [ ] T005 Add `ScanStatus`, `ScanContentHash`, `ScanUpdatedAtUtc`, and user-safe `ScanError` properties to `ContosoDashboard/Models/Document.cs` with the exact allowed states `PendingScan`, `Clean`, `Quarantined`, and `ScanFailed`.
- [ ] T006 Update `ContosoDashboard/Data/ApplicationDbContext.cs` to configure document scan fields, useful indexes, and SQLite-compatible persistence for the scan lifecycle.
- [ ] T007 Create the versioned scan message contract in `ContosoDashboard/Services/DocumentScanMessage.cs` with `schemaVersion`, `documentId`, `storedFilePath`, `contentHash`, and `uploadedAtUtc` fields.
- [ ] T008 [P] Create the queue publishing abstraction and Azure Queue Storage implementation in `ContosoDashboard/Services/IDocumentScanQueue.cs` and `ContosoDashboard/Services/AzureDocumentScanQueue.cs`, including configuration-based enablement and no-secret logging.
- [ ] T009 [P] Create the local disabled/deterministic queue fallback in `ContosoDashboard/Services/LocalDocumentScanQueue.cs` and its configuration contract so offline training does not require Azure resources.
- [ ] T010 Update `ContosoDashboard/Services/IFileStorageService.cs` and `ContosoDashboard/Services/LocalFileStorageService.cs` to expose a safe worker-readable storage identifier and content-hash calculation without exposing public URLs.
- [ ] T011 Refactor shared authorization and scan-status checks in `ContosoDashboard/Services/DocumentService.cs` so list, detail, download, preview, share, replace, and delete operations exclude deleted documents and block non-`Clean` files from download/preview.
- [ ] T012 Register the scan queue implementation and related configuration in `ContosoDashboard/Program.cs`, defaulting to the offline local adapter when Azure integration is not enabled.

**Checkpoint**: The database, queue boundary, storage boundary, and authorization rules are ready before story work begins.

---

## Phase 3: User Story 1 - Upload and organize work documents (Priority: P1) MVP

**Goal**: Authenticated employees can upload supported files with metadata, persist them securely as pending scans, and receive clear validation/status feedback.

**Independent Test**: Upload a supported file as an authenticated user, verify metadata and `PendingScan` state, reject an unsupported extension and a file over 25 MB, and confirm no orphaned file or unauthorized download is possible.

### Implementation

- [ ] T013 [US1] Update `ContosoDashboard/Services/DocumentService.cs` to validate the 25 MB limit, extension allowlist, sanitized display name, required title/category, and optional project association before storage.
- [ ] T014 [US1] Update `ContosoDashboard/Services/DocumentService.cs` to persist uploaded documents as `PendingScan`, compute and store the content hash, publish exactly one scan message only after file/metadata persistence succeeds, and clean up storage on persistence failure.
- [ ] T015 [US1] Update `ContosoDashboard/Services/DocumentService.cs` to record upload audit events and ensure project notifications do not imply that a non-clean document is downloadable.
- [ ] T016 [US1] Update `ContosoDashboard/Pages/Documents.razor` to show upload progress/busy state, validation errors, `PendingScan`/terminal scan status, and a clear success message after the upload request completes.
- [ ] T017 [US1] Update `ContosoDashboard/Models/Document.cs` and `ContosoDashboard/Pages/Documents.razor` to support optional description, project association, tags, and user-visible scan-safe metadata without exposing the physical storage path.
- [ ] T018 [US1] Update `ContosoDashboard/Pages/Documents.razor` to disable download/preview actions until the document scan status is `Clean` and present safe messaging for `Quarantined` and `ScanFailed` files.

**Checkpoint**: User Story 1 is independently demonstrable with local storage and the offline queue/scanner configuration.

---

## Phase 4: User Story 2 - Browse, filter, search, and access project documents (Priority: P2)

**Goal**: Authorized users can find accessible documents by project, category, metadata, and sorting while project members receive default access to project documents.

**Independent Test**: Sign in as a seeded project member, find a project document through search/filter/project navigation, download it after a `Clean` result, and verify an unauthorized user cannot list, view, download, or preview it.

### Implementation

- [ ] T019 [US2] Extend `ContosoDashboard/Services/DocumentService.cs` accessible-document queries to search title, description, tags, file name, uploader, and project, while applying authorization before returning results.
- [ ] T020 [US2] Extend `ContosoDashboard/Services/DocumentService.cs` with sorting and filtering by title, upload date, category, file size, and project association using server-side query composition.
- [ ] T021 [US2] Update `ContosoDashboard/Pages/Documents.razor` to provide category/project filters, sorting controls, search feedback, scan-status display, and metadata columns for authorized documents.
- [ ] T022 [US2] Update `ContosoDashboard/Pages/ProjectDetails.razor` to load project documents through `IDocumentService`, preserve project-member authorization, and avoid direct file links.
- [ ] T023 [US2] Add authorized document detail and browser-viewable preview handling in `ContosoDashboard/Pages/DocumentDetails.razor` and the corresponding `ContosoDashboard/Services/DocumentService.cs` operation, allowing preview/download only for `Clean` documents.
- [ ] T024 [US2] Update `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Pages/DocumentDetails.razor` to return safe not-found/forbidden behavior without disclosing metadata for unauthorized document IDs.

**Checkpoint**: User Stories 1 and 2 work independently with owner, project-member, explicit-access, and blocked-scan cases.

---

## Phase 5: User Story 3 - Share, notify, and manage access (Priority: P3)

**Goal**: Owners and project managers can manage document lifecycle and sharing, while recipients receive notifications and authorized actions are audited.

**Independent Test**: Share a clean document as its owner, verify the recipient notification and shared-with-me result, update/replace metadata as owner, delete as owner or project manager, and verify unauthorized mutations fail.

### Implementation

- [ ] T025 [P] [US3] Extend `ContosoDashboard/Services/DocumentService.cs` with owner-only metadata update and file replacement operations that reset replacement files to `PendingScan`, preserve the old file on failure, and remove it only after successful persistence.
- [ ] T026 [P] [US3] Extend `ContosoDashboard/Services/DocumentService.cs` with active-share listing/revocation or equivalent access-management operations, preventing self-share and duplicate active shares.
- [ ] T027 [US3] Update `ContosoDashboard/Pages/DocumentDetails.razor` and `ContosoDashboard/Pages/Documents.razor` with owner/project-manager edit, replace, share, revoke, and delete controls using confirmation and scan-status-safe actions.
- [ ] T028 [US3] Update `ContosoDashboard/Services/DocumentService.cs` to audit share, replacement, metadata-update, download, delete, and scan-terminal events and send in-app notifications through `INotificationService`.
- [ ] T029 [US3] Add the shared-with-me view and notification state in `ContosoDashboard/Pages/SharedDocuments.razor` and `ContosoDashboard/Shared/NavMenu.razor`, using only `IDocumentService` results.
- [ ] T030 [US3] Update `ContosoDashboard/Services/DocumentService.cs` to ensure authorized deletion removes the physical file, retains audit history, invalidates active access, and rejects unauthorized deletion without state changes.

**Checkpoint**: All three user stories are independently usable and share the same server-side authorization predicate.

---

## Phase 6: Asynchronous Virus Scan Worker

**Purpose**: Deliver the background processing required by FR-023 and FR-024 without coupling scan execution to the Blazor upload request.

- [ ] T031 Create `ContosoDashboard.Scanner/Services/IAntivirusScanner.cs` with clean, malware, and transient-error result semantics that do not expose scanner implementation details to callers.
- [ ] T032 [P] Create the configured production scanner adapter in `ContosoDashboard.Scanner/Services/AntivirusScanner.cs` and the deterministic offline scanner adapter in `ContosoDashboard.Scanner/Services/DeterministicAntivirusScanner.cs`.
- [ ] T033 Implement the Azure Queue Storage trigger in `ContosoDashboard.Scanner/Functions/DocumentScanFunction.cs` to deserialize and validate the versioned message, retrieve the file, hash/compare content, invoke `IAntivirusScanner`, and update only the matching document.
- [ ] T034 Configure bounded retries, poison/dead-letter handling, structured correlation logging, and no file-content logging in `ContosoDashboard.Scanner/host.json` and `ContosoDashboard.Scanner/Functions/DocumentScanFunction.cs`.
- [ ] T035 Implement idempotent terminal-state and audit updates through `ContosoDashboard.Scanner/Services/IDocumentScanResultStore.cs` and `ContosoDashboard.Scanner/Services/DocumentScanResultStore.cs`, preventing duplicate audit events and stale status regressions.
- [ ] T036 Add the scanner project to `ContosoDashboard.sln` or the repository solution configuration and document local/Azure startup commands in `README.md` and `specs/002-document-upload-management/quickstart.md`.

**Checkpoint**: Duplicate queue deliveries, malware results, transient failures, dead-letter behavior, and blocked downloads are all covered by the documented validation flow.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Complete documentation, security review, and final validation across the feature.

- [ ] T037 [P] Update `ContosoDashboard/Pages/Index.razor` and `ContosoDashboard/Services/DashboardService.cs` with recent-document and scan-status-safe document summaries.
- [ ] T038 [P] Update `ContosoDashboard/Pages/Tasks.razor`, `ContosoDashboard/Pages/ProjectDetails.razor`, and `ContosoDashboard/Shared/NavMenu.razor` to integrate document navigation without duplicating authorization logic.
- [ ] T039 Review `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Services/LocalFileStorageService.cs`, and `ContosoDashboard.Scanner/Functions/DocumentScanFunction.cs` for path traversal, direct-file exposure, unsafe logging, stale queue messages, and unauthorized object access.
- [ ] T040 Update `ContosoDashboard/Data/ApplicationDbContext.cs` and repository documentation for SQLite schema initialization/migration behavior, scan status persistence, and the Azure deployment profile.
- [ ] T041 Run `dotnet build --configuration Debug` for `ContosoDashboard/ContosoDashboard.csproj` and the scanner project, then resolve build errors and document any non-blocking warnings.
- [ ] T042 Run every scenario in `specs/002-document-upload-management/quickstart.md`, including local fallback and Azure Queue Storage scan-worker paths, and record the results in the feature review.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; T001-T004 can begin immediately in parallel where files differ.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories because scan status, queue publication, storage identity, and authorization are shared prerequisites.
- **User Story 1 (Phase 3)**: Depends on T005-T012; MVP upload flow can be delivered independently.
- **User Story 2 (Phase 4)**: Depends on T005-T012 and the document data/service contracts; it can proceed in parallel with US1 after the foundation, but T019-T024 should integrate with the final US1 service signatures.
- **User Story 3 (Phase 5)**: Depends on T005-T012 and the document service contract; it can proceed in parallel with US1/US2 after the foundation.
- **Asynchronous Virus Scan Worker (Phase 6)**: Depends on T005-T012 and T014's queue message contract; UI enforcement tasks should be complete before declaring scan safety finished.
- **Polish (Phase 7)**: Depends on all desired stories and the scan worker.

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational; MVP.
- **US2 (P2)**: Depends only on Foundational at the architecture level; consumes the upload/document contract.
- **US3 (P3)**: Depends only on Foundational at the architecture level; consumes the document access and notification contracts.
- **Scan Worker**: Depends on Foundational and the US1 queue publication behavior; its result-store contract must be available before worker implementation is considered complete.

### Within Each Story

- Implement models and shared contracts before service changes.
- Implement service authorization and persistence before Razor UI integration.
- Complete the story checkpoint before treating it as independently deliverable.
- Do not expose a file stream or direct URL before the `Clean` scan invariant is enforced.

## Parallel Execution Examples

### After Foundational Phase

```text
Developer A: T013-T018 [US1] upload validation, persistence, and upload UI
Developer B: T019-T024 [US2] search, filtering, project access, and clean-file preview
Developer C: T025-T030 [US3] sharing, notifications, replacement, and deletion
Developer D: T031-T036 scan worker, queue trigger, retries, and idempotent results
```

### MVP Slice

```text
T005-T012 -> T013-T018 -> T041 -> T042 (US1 local/offline validation)
```

## Implementation Strategy

### MVP First

1. Complete Setup and Foundational phases.
2. Implement User Story 1 with the offline queue/scanner adapter.
3. Validate upload metadata, rejection paths, pending/clean status, and download blocking.
4. Stop for an MVP review before adding search, sharing, or Azure deployment integration.

### Incremental Delivery

1. Add User Story 2 for authorized discovery and clean-file retrieval.
2. Add User Story 3 for sharing, notifications, replacement, and deletion.
3. Add the Azure Functions worker and Queue Storage deployment path.
4. Run cross-cutting security and quickstart validation.

## Notes

- Every task uses the required checklist format: checkbox, sequential ID, optional `[P]`, required story label for user-story tasks, and an exact file path.
- No automated test tasks were added because the feature specification does not explicitly request TDD or a test framework; the quickstart and build validation remain required completion checks.
- The Azure worker is intentionally isolated from the Blazor project so offline training remains possible without Azure resources.
