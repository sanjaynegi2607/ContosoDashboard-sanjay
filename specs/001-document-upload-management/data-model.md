# Data Model: Document Upload and Management

## Core entities

### Document

Represents a single uploaded file and associated metadata.

**Key fields**
- DocumentId: integer
- Title: string
- Description: string?
- Category: string
- FileName: string
- StoredFilePath: string
- FileType: string
- FileSizeBytes: long
- UploadedByUserId: integer
- ProjectId: integer?
- UploadedAtUtc: DateTime
- UpdatedAtUtc: DateTime?
- IsDeleted: bool

**Relationships**
- Many documents belong to one user (uploader)
- Many documents may belong to one project
- One document may have many share records
- One document may have many audit log entries

### DocumentShare

Tracks explicit user-to-document access.

**Key fields**
- DocumentShareId: integer
- DocumentId: integer
- UserId: integer
- SharedByUserId: integer
- SharedAtUtc: DateTime
- IsActive: bool

### UploadAuditLog

Tracks document events for audit reporting.

**Key fields**
- AuditLogId: integer
- DocumentId: integer?
- UserId: integer
- ActionType: string
- ActionDateUtc: DateTime
- Details: string?

## Existing repository alignment

The existing `ApplicationDbContext` already manages user, project, and notification entities. The document feature should extend that context by adding `DbSet<Document>`, `DbSet<DocumentShare>`, and `DbSet<UploadAuditLog>` in the same pattern as the current model definitions.
