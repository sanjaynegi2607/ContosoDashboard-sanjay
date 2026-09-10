namespace ContosoDashboard.Services;

public interface IDocumentScanQueue
{
    bool IsEnabled { get; }
    Task PublishAsync(DocumentScanMessage message, CancellationToken cancellationToken = default);
}
