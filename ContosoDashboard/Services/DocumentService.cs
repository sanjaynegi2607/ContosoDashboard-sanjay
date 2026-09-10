using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<List<Document>> GetAccessibleDocumentsAsync(int requestingUserId, int? projectId = null, string? search = null, string? category = null);
    Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId);
    Task<Document> UploadDocumentAsync(Document document, Stream fileStream, string originalFileName, int requestingUserId, string? contentType = null);
    Task<Stream> DownloadDocumentAsync(int documentId, int requestingUserId);
    Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId);
    Task<bool> ShareDocumentAsync(int documentId, int targetUserId, int requestingUserId);
    Task<List<Document>> GetSharedWithUserAsync(int userId);
    Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId);
    Task<List<User>> GetAvailableUsersForSharingAsync(int requestingUserId);
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".rtf", ".jpg", ".jpeg", ".png", ".gif", ".bmp",
        ".webp"
    };

    public DocumentService(ApplicationDbContext context, IFileStorageService fileStorageService, INotificationService notificationService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
    }

    public async Task<List<Document>> GetAccessibleDocumentsAsync(int requestingUserId, int? projectId = null, string? search = null, string? category = null)
    {
        var query = _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.Shares)
            .Where(d => !d.IsDeleted)
            .Where(d =>
                d.UploadedByUserId == requestingUserId ||
                d.ProjectId.HasValue && d.Project.ProjectMembers.Any(pm => pm.UserId == requestingUserId) ||
                d.Shares.Any(s => s.UserId == requestingUserId && s.IsActive));

        if (projectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(d => d.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.Title.Contains(term) ||
                d.Description != null && d.Description.Contains(term) ||
                d.FileName.Contains(term) ||
                d.UploadedByUser.DisplayName.Contains(term) ||
                (d.Project != null && d.Project.Name.Contains(term)));
        }

        return await query
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.Shares)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null)
        {
            return null;
        }

        var isOwner = document.UploadedByUserId == requestingUserId;
        var isProjectMember = document.ProjectId.HasValue &&
            await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == document.ProjectId.Value && pm.UserId == requestingUserId);
        var isShared = document.Shares.Any(s => s.UserId == requestingUserId && s.IsActive);

        if (!isOwner && !isProjectMember && !isShared)
        {
            return null;
        }

        return document;
    }

    public async Task<Document> UploadDocumentAsync(Document document, Stream fileStream, string originalFileName, int requestingUserId, string? contentType = null)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (fileStream == null)
            throw new ArgumentException("A file is required.", nameof(fileStream));

        if (string.IsNullOrWhiteSpace(document.Title))
            throw new InvalidOperationException("Document title is required.");

        if (string.IsNullOrWhiteSpace(document.Category))
            throw new InvalidOperationException("Document category is required.");

        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Unsupported file type. Allowed formats: PDF, Word, Excel, PowerPoint, text, and common image files.");
        }

        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("The selected file exceeds the 25 MB limit.");
        }

        var cleanedName = Path.GetFileName(originalFileName);
        if (string.IsNullOrWhiteSpace(cleanedName) || cleanedName.Contains(".."))
        {
            throw new InvalidOperationException("Invalid file name.");
        }

        var safeUniquePath = await _fileStorageService.UploadAsync(fileStream, originalFileName, contentType ?? "application/octet-stream");

        document.Title = document.Title.Trim();
        document.FileName = cleanedName;
        document.StoredFilePath = safeUniquePath;
        document.FileType = extension.TrimStart('.');
        document.FileSizeBytes = fileStream.Length;
        document.UploadedByUserId = requestingUserId;
        document.UploadedAtUtc = DateTime.UtcNow;
        document.UpdatedAtUtc = DateTime.UtcNow;
        document.IsDeleted = false;

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _context.UploadAuditLogs.Add(new UploadAuditLog
        {
            DocumentId = document.DocumentId,
            UserId = requestingUserId,
            ActionType = "Upload",
            ActionDateUtc = DateTime.UtcNow,
            Details = $"Uploaded {document.FileName}"
        });

        await _context.SaveChangesAsync();

        if (document.ProjectId.HasValue)
        {
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = requestingUserId,
                Title = "Document uploaded",
                Message = $"Your file '{document.Title}' was uploaded to the project.",
                Type = NotificationType.ProjectUpdate,
                Priority = NotificationPriority.Informational
            });

            var projectMembers = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == document.ProjectId.Value && pm.UserId != requestingUserId)
                .Select(pm => pm.UserId)
                .ToListAsync();

            foreach (var memberId in projectMembers)
            {
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = memberId,
                    Title = "New project document",
                    Message = $"A new document '{document.Title}' is available in the project.",
                    Type = NotificationType.ProjectUpdate,
                    Priority = NotificationPriority.Informational
                });
            }
        }

        return document;
    }

    public async Task<Stream> DownloadDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await GetDocumentByIdAsync(documentId, requestingUserId);
        if (document == null)
        {
            throw new UnauthorizedAccessException("You do not have access to this document.");
        }

        var stream = await _fileStorageService.DownloadAsync(document.StoredFilePath);

        _context.UploadAuditLogs.Add(new UploadAuditLog
        {
            DocumentId = document.DocumentId,
            UserId = requestingUserId,
            ActionType = "Download",
            ActionDateUtc = DateTime.UtcNow,
            Details = $"Downloaded document {document.Title}"
        });

        await _context.SaveChangesAsync();
        return stream;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null)
        {
            return false;
        }

        var isOwner = document.UploadedByUserId == requestingUserId;
        var isProjectManager = document.ProjectId.HasValue && document.Project != null && document.Project.ProjectManagerId == requestingUserId;

        if (!isOwner && !isProjectManager)
        {
            return false;
        }

        document.IsDeleted = true;
        document.UpdatedAtUtc = DateTime.UtcNow;

        _context.UploadAuditLogs.Add(new UploadAuditLog
        {
            DocumentId = document.DocumentId,
            UserId = requestingUserId,
            ActionType = "Delete",
            ActionDateUtc = DateTime.UtcNow,
            Details = $"Deleted document {document.Title}"
        });

        await _fileStorageService.DeleteAsync(document.StoredFilePath);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ShareDocumentAsync(int documentId, int targetUserId, int requestingUserId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null || document.UploadedByUserId != requestingUserId)
        {
            return false;
        }

        if (targetUserId == requestingUserId)
        {
            return false;
        }

        var existing = await _context.DocumentShares
            .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.UserId == targetUserId && s.IsActive);

        if (existing != null)
        {
            return false;
        }

        var share = new DocumentShare
        {
            DocumentId = documentId,
            UserId = targetUserId,
            SharedByUserId = requestingUserId,
            SharedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        _context.DocumentShares.Add(share);
        _context.UploadAuditLogs.Add(new UploadAuditLog
        {
            DocumentId = documentId,
            UserId = requestingUserId,
            ActionType = "Share",
            ActionDateUtc = DateTime.UtcNow,
            Details = $"Shared document with user {targetUserId}"
        });

        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(new Notification
        {
            UserId = targetUserId,
            Title = "Document shared with you",
            Message = $"A document titled '{document.Title}' has been shared with you.",
            Type = NotificationType.ProjectUpdate,
            Priority = NotificationPriority.Important
        });

        return true;
    }

    public async Task<List<Document>> GetSharedWithUserAsync(int userId)
    {
        return await _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Where(d => !d.IsDeleted && d.Shares.Any(s => s.UserId == userId && s.IsActive))
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();
    }

    public async Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId)
    {
        return await GetAccessibleDocumentsAsync(requestingUserId, projectId);
    }

    public async Task<List<User>> GetAvailableUsersForSharingAsync(int requestingUserId)
    {
        return await _context.Users
            .Where(u => u.UserId != requestingUserId)
            .OrderBy(u => u.DisplayName)
            .ToListAsync();
    }
}
