# Document Management Contract

This contract describes the application-facing behavior for the document-management service, Razor UI, and asynchronous scan worker. It is an internal application contract, not a public HTTP API.

## Virus-Scan Processing Contract

After a file and its metadata are persisted successfully, the web application MUST publish one queue message when scanning is enabled. The Azure Functions worker consumes the message using an Azure Queue Storage trigger.

Queue message fields:

| Field | Type | Rule |
|---|---|---|
| `schemaVersion` | string | Required; permits safe message evolution. |
| `documentId` | integer | Required SQLite document identifier. |
| `storedFilePath` | string | Required storage identifier resolved through the storage boundary; never a public URL. |
| `contentHash` | string | Required hash used for idempotency and stale-message detection. |
| `uploadedAtUtc` | string | Required UTC timestamp for tracing. |

Processing rules:

- The worker MUST be idempotent for duplicate deliveries and MUST NOT duplicate terminal audit events.
- The worker MUST retrieve the file through an approved storage adapter, invoke `IAntivirusScanner`, and update the document status only if the content hash still matches.
- Clean results set status to `Clean`; malware results set status to `Quarantined`; transient or terminal scanner errors set status to `ScanFailed` after retry policy is exhausted.
- `PendingScan`, `Quarantined`, and `ScanFailed` documents MUST NOT be downloadable or previewable.
- Azure Queue retry handling MUST use bounded retries and a poison/dead-letter queue. The worker MUST log correlation data without logging file contents or sensitive payloads.
- Local training MUST support a disabled or deterministic test scanner and local queue adapter so Azure resources are not required.

## Authorization Invariant

Every operation that returns or mutates a document MUST evaluate the requesting user on the server. A document is visible when the user is the owner, a member of its associated project, or an active explicit share recipient. Unauthorized detail and download requests behave as not found or unauthorized and MUST NOT disclose file metadata.

## Service Operations

| Operation | Inputs | Success behavior | Failure behavior |
|---|---|---|---|
| List accessible documents | requesting user, optional project/category/search/sort filters | Returns non-deleted documents within the access predicate, including display metadata. | Empty result for no matches; never returns unauthorized rows. |
| Get document | document ID, requesting user | Returns document metadata and permitted actions. | Returns no document for missing, deleted, or unauthorized records. |
| Upload document | metadata, file stream, original name, requesting user, content type | Validates metadata, extension, size, and safe name; stores file; persists metadata as `PendingScan`; publishes a scan message; writes audit record; notifies relevant project members. | Clear validation error; no database record or orphaned file after a failed operation. |
| Download/preview document | document ID, requesting user | Returns a stream only after authorization and writes a download audit record. | Access error for unauthorized users; missing-file error for absent storage content. |
| Update metadata | document ID, metadata, requesting user | Owner updates title, description, category, project, and tags where supported. | Rejects unauthorized, invalid, deleted, or invalid-project changes. |
| Replace file | document ID, new file, requesting user | Validates and stores replacement, updates file metadata, removes old file after successful persistence, and audits the change. | Rejects invalid file; retains prior document state when replacement fails. |
| Delete document | document ID, requesting user | Owner or project manager marks metadata deleted, removes physical file, and writes delete audit record. | Returns failure without changing data for unauthorized or missing documents. |
| Share document | document ID, target user, requesting user | Owner creates one active share, writes audit record, and sends in-app notification. | Rejects self-share, duplicate active share, missing document, or unauthorized actor. |
| List shared documents | requesting user | Returns active, non-deleted documents explicitly shared with the user. | Empty result when no shares exist. |

## Upload Rules

- Allowed extensions: `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.txt`, `.csv`, `.rtf`, `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`.
- Maximum size: 25 MiB (25 * 1024 * 1024 bytes).
- Storage name: generated GUID plus validated extension.
- Storage location: application data directory outside `wwwroot`.
- User-provided names are display metadata only and MUST NOT select the physical path.

## UI Contract

The documents page MUST provide:

- Authenticated access only.
- Upload fields for title, category, optional description, optional project, and file selection.
- Clear progress/busy state and success/error result.
- Search and filters for accessible documents.
- Document metadata showing title, category, project, upload date, and size.
- Authorized download/preview only after a `Clean` scan status; share, edit, replace, and delete actions where implemented.
- No direct public file URL.

Dashboard and project views MUST use the same service authorization rules and expose recent/count or project-document information without duplicating access logic.

## Audit and Notification Contract

The service MUST record upload, download, delete, share, and terminal scan actions with UTC timestamps and actor IDs or worker identity. A project upload notifies eligible project members other than the uploader. A direct share notifies the recipient through the existing in-app notification service.
