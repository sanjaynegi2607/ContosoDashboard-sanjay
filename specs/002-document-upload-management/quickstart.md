# Quickstart Validation: Document Upload and Management

## Prerequisites

- .NET 10 SDK installed and available through `dotnet --version`.
- Windows local development environment; no SQL Server or cloud account required.
- Run commands from the repository root.

## Build Validation

```powershell
cd ContosoDashboard
dotnet restore
dotnet build --configuration Debug
```

Expected result: build succeeds for `net10.0`. Warnings may be reported, but errors must be resolved before completion.

## Run the Application

```powershell
dotnet run
```

Open the HTTPS URL printed by the application and sign in through `/login`. The seeded users are documented in [README.md](../../README.md).

## Scenario 1: Upload and metadata

1. Sign in as `ni.kang@contoso.com`.
2. Open `/documents` and upload a small supported PDF or text file.
3. Enter a title and category; optionally associate it with project `1`.
4. Confirm a success message and verify title, category, project, date, size, and `PendingScan` status appear in the list.
5. Confirm the physical file is under the application data upload directory and not under `wwwroot`.
6. With the Azure queue integration enabled, confirm a scan message is published only after the upload transaction succeeds and the document becomes `Clean` before download is enabled.

Expected result: one accessible document row and one upload audit event.

## Scenario 2: Asynchronous scan worker

1. Run the Azure Functions scanner with an Azure Storage Queue connection, or select the local queue adapter for offline validation.
2. Place a valid scan message from [contracts/document-management.md](contracts/document-management.md) on the upload queue.
3. Confirm the function retrieves the file, invokes the scanner adapter, and changes the document from `PendingScan` to `Clean`.
4. Submit the same message twice and confirm the terminal status and audit record are not duplicated.
5. Exercise a deterministic malware result and a transient scanner error. Confirm malware produces `Quarantined`, transient failures retry, and exhausted retries move the message to the poison/dead-letter queue and the document to `ScanFailed`.
6. Confirm `PendingScan`, `Quarantined`, and `ScanFailed` documents cannot be downloaded or previewed.

## Scenario 3: Validation and failure cleanup

1. Attempt an unsupported extension such as `.exe`.
2. Attempt a file larger than 25 MB.
3. Confirm each attempt reports a clear error and does not create a visible document or orphaned stored file.

## Scenario 4: Project-member access

1. Upload a document associated with project `1` as `ni.kang@contoso.com`.
2. Sign out and sign in as `floris.kregel@contoso.com`, a seeded project member.
3. Open `/documents` and project details.
4. Confirm the project document is visible and downloadable.
5. Sign in as a user who is neither a project member, owner, nor explicit share recipient and confirm the document is absent and direct access is denied.

## Scenario 5: Search and filtering

1. Create documents with different titles, categories, and project associations.
2. Search by title, description, uploader, and project name.
3. Filter by category and project; exercise sorting by title, date, and size where the UI provides it.

Expected result: only matching documents within the current user's authorized set are returned.

## Scenario 6: Sharing and notifications

1. As the owner, share a document with another seeded user.
2. Sign in as the recipient and open the shared-with-me view.
3. Confirm the document is visible and an in-app notification exists.
4. Attempt the same share as a non-owner and confirm it is rejected.

## Scenario 7: Delete and audit

1. As the owner or project manager, delete a document after confirmation.
2. Confirm it disappears from all normal lists and its physical file is removed.
3. Confirm the audit record remains available to the application for reporting.
4. Attempt deletion as an unauthorized user and confirm no state changes occur.

## Design References

- Entity rules: [data-model.md](data-model.md)
- Service/UI contract: [contracts/document-management.md](contracts/document-management.md)
- Requirements: [spec.md](spec.md)
