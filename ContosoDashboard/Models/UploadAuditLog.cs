using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class UploadAuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    public int? DocumentId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;

    public DateTime ActionDateUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? Details { get; set; }

    [ForeignKey("DocumentId")]
    public virtual Document? Document { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
