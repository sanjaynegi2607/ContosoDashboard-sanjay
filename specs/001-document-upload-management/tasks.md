# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

## Phase 1: Setup and shared infrastructure

- [ ] T001 Create the feature directory structure under `specs/001-document-upload-management` and initialize the related contract folder.
- [ ] T002 Review the existing project architecture in `ContosoDashboard/Data`, `ContosoDashboard/Models`, `ContosoDashboard/Services`, and `ContosoDashboard/Pages` to confirm integration points.
- [ ] T003 [P] Add the required new model classes for `Document`, `DocumentShare`, and `UploadAuditLog` in `ContosoDashboard/Models/`.
- [ ] T004 [P] Register the new DbSets and relationship mappings in `ContosoDashboard/Data/ApplicationDbContext.cs`.

## Phase 2: Foundational document infrastructure

**Purpose**: This phase blocks all user-story work until the storage contract and access rules are present.

- [ ] T005 Create the `IFileStorageService` abstraction in `ContosoDashboard/Services/` with `UploadAsync`, `DeleteAsync`, `DownloadAsync`, and `GetUrlAsync` style methods.
- [ ] T006 Implement the local storage provider in `ContosoDashboard/Services/LocalFileStorageService.cs` using the safe GUID-based storage pattern described in the requirements.
- [ ] T007 Add dependency injection registration for the storage service and service-layer document operations in `ContosoDashboard/Program.cs`.
- [ ] T008 Create `DocumentService` in `ContosoDashboard/Services/` to validate uploads, enforce authorization rules, persist metadata, and coordinate file-save operations.
- [ ] T009 [P] Add a notification integration path so new project documents and shared documents trigger the existing notification workflow.
- [ ] T010 Create a document access validation flow for project membership, document ownership, and share-based permissions.

**Checkpoint**: Document storage and access controls are ready before any end-user UI flows are implemented.

## Phase 3: User Story 1 - Upload and organize documents (Priority: P1)

**Goal**: Users can upload valid documents with metadata and have them stored securely.

**Independent Test**: A user logs in, uploads a valid PDF, fills in the title and category, and confirms the file is saved with metadata and visible in their document list.

### Implementation for User Story 1

- [ ] T011 [P] Add a `Document` model with the required fields and constraints in `ContosoDashboard/Models/Document.cs`.
- [ ] T012 [P] Add a `DocumentShare` model for user access grants in `ContosoDashboard/Models/DocumentShare.cs`.
- [ ] T013 [P] Add an `UploadAuditLog` model in `ContosoDashboard/Models/UploadAuditLog.cs`.
- [ ] T014 Implement upload validation and metadata persistence in `DocumentService` for size, extension, category, user, and project checks.
- [ ] T015 Add the upload UI flow in `ContosoDashboard/Pages/Documents.razor` or a dedicated document page, with file selection, metadata entry, and success/error messaging.
- [ ] T016 Ensure the upload process creates a unique physical path before the database record is saved, matching the offline safety requirement.
- [ ] T017 Add document list sorting, filtering, and summary display for the user’s uploads in the documents page.

**Checkpoint**: User Story 1 is independently testable and delivers upload value without waiting for share or dashboard work.

## Phase 4: User Story 2 - Browse, filter, search, and project document access (Priority: P2)

**Goal**: Users can locate and access documents across personal and project contexts.

**Independent Test**: A project member can search by keyword, filter by project/category, and download a permitted document from the project or personal document views.

### Implementation for User Story 2

- [ ] T018 [P] Extend project pages in `ContosoDashboard/Pages/ProjectDetails.razor` to display associated documents and upload actions.
- [ ] T019 [P] Add a search and filter experience in the main documents page for title, description, tag, project, and category.
- [ ] T020 Implement document retrieval and access checks in `DocumentService` for the current user and project membership.
- [ ] T021 Add download and preview actions for accessible files, including browser-supported document previews when possible.
- [ ] T022 Add shared-with-me and personal list views so the same document can appear in the correct user-specific lists.
- [ ] T023 Update the home dashboard in `ContosoDashboard/Pages/Index.razor` to include recent documents and summary counts.

**Checkpoint**: The feature is discoverable and usable by project teams without needing sharing flows.

## Phase 5: User Story 3 - Share and manage document access (Priority: P3)

**Goal**: Users can share documents and receive notifications.

**Independent Test**: A user shares a document with another employee, and the recipient sees the notification and the document in their shared list.

### Implementation for User Story 3

- [ ] T024 Add a share workflow and recipient selection to the document detail or document management UI.
- [ ] T025 Persist `DocumentShare` records and ensure they are respected by the access rules in `DocumentService`.
- [ ] T026 Trigger in-app notifications for document shares and project additions using the existing notification service patterns.
- [ ] T027 Add delete confirmation, permanent file removal, and metadata cleanup flows for owners and project managers.
- [ ] T028 Implement audit logging for upload, download, delete, and share actions using `UploadAuditLog` and the current logging conventions.
- [ ] T029 Verify that unauthorized users cannot access protected documents or direct file URLs.

**Checkpoint**: All user stories are now independently functional and the feature is production-ready for the training context.

## Final validation

- [ ] T030 Run a local build and verify the application still starts with the new document infrastructure.
- [ ] T031 Validate the upload, search, project view, and notification workflows using the existing mock authentication users.
- [ ] T032 Update any relevant documentation or quickstart guidance in the feature folder and the repository README if required.
