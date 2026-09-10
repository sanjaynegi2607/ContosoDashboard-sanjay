namespace ContosoDashboard.Services;

public sealed class LocalDocumentScanQueue : IDocumentScanQueue
{
    public bool IsEnabled => false;

    public Task PublishAsync(DocumentScanMessage message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
