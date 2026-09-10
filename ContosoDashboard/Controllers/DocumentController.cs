using System.Security.Claims;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContosoDashboard.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentController : ControllerBase
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IDocumentService documentService, ILogger<DocumentController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSizeBytes + (1024 * 1024))]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> UploadAsync(
        [FromForm] string? title,
        [FromForm] string? category,
        [FromForm] string? description,
        [FromForm] int? projectId,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequestingUserId(out var requestingUserId))
        {
            return Unauthorized();
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "A non-empty file is required." });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                error = "The selected file exceeds the 25 MB limit."
            });
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest(new { error = "Document title is required." });
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return BadRequest(new { error = "Document category is required." });
        }

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            return BadRequest(new { error = "A valid file name is required." });
        }

        try
        {
            var document = new Document
            {
                Title = title,
                Category = category,
                Description = description,
                ProjectId = projectId
            };

            await using var fileStream = file.OpenReadStream();
            var uploadedDocument = await _documentService.UploadDocumentAsync(
                document,
                fileStream,
                file.FileName,
                requestingUserId,
                file.ContentType);

            return Created($"/api/documents/{uploadedDocument.DocumentId}", new DocumentUploadResponse(
                uploadedDocument.DocumentId,
                uploadedDocument.Title,
                uploadedDocument.Category,
                uploadedDocument.FileName,
                uploadedDocument.FileSizeBytes,
                uploadedDocument.ProjectId,
                uploadedDocument.UploadedAtUtc));
        }
        catch (InvalidOperationException exception) when (IsUnsupportedFileType(exception.Message))
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new { error = exception.Message });
        }
        catch (InvalidOperationException exception) when (IsFileTooLarge(exception.Message))
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = exception.Message });
        }
        catch (InvalidOperationException exception) when (IsClientValidationError(exception.Message))
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Document upload failed for authenticated user {UserId}.", requestingUserId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Document upload failed",
                detail: "The document could not be uploaded. Please try again later.");
        }
    }

    private bool TryGetRequestingUserId(out int userId)
    {
        userId = 0;
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out userId) && userId > 0;
    }

    private static bool IsUnsupportedFileType(string message) =>
        message.StartsWith("Unsupported file type", StringComparison.OrdinalIgnoreCase);

    private static bool IsFileTooLarge(string message) =>
        message.Contains("25 MB limit", StringComparison.OrdinalIgnoreCase);

    private static bool IsClientValidationError(string message) =>
        message.Contains("required", StringComparison.OrdinalIgnoreCase) ||
        message.StartsWith("Invalid file name", StringComparison.OrdinalIgnoreCase);

    public sealed record DocumentUploadResponse(
        int DocumentId,
        string Title,
        string Category,
        string FileName,
        long FileSizeBytes,
        int? ProjectId,
        DateTime UploadedAtUtc);
}
