# Document Management Contract

## Upload document

**Operation**: POST `/documents/upload`

**Request**
- multipart form data
- file: binary content
- title: string
- description: optional string
- category: string
- projectId: optional integer
- tags: optional string[]

**Behavior**
- Validate file extension and size
- Generate a GUID-based safe stored path
- Save file to local storage
- Persist metadata to the database
- Trigger project or user notifications if required

**Response**
- 200/201 on success
- 400 for invalid file type or size
- 403 for unauthorized project upload

## Download document

**Operation**: GET `/documents/{documentId}/download`

**Behavior**
- Verify document exists
- Check permission based on ownership, project membership, or shared access
- Stream the file back to the user

**Response**
- 200 with file content
- 403 for unauthorized access
- 404 for missing document

## Share document

**Operation**: POST `/documents/{documentId}/share`

**Request**
- userId: integer

**Behavior**
- Validate ownership or project-manager permission
- Create a `DocumentShare` record
- Notify the recipient

**Response**
- 200 on success
- 403 for unauthorized share action
