using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;

namespace ContosoDashboard.Services;

public sealed class AzureDocumentScanQueue : IDocumentScanQueue
{
    private readonly QueueClient _queueClient;
    private readonly bool _enabled;

    public AzureDocumentScanQueue(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage") ??
            configuration["AzureStorage:ConnectionString"];
        var queueName = configuration["AzureStorage:ScanQueueName"] ?? "document-scans";
        _enabled = configuration.GetValue("AzureStorage:ScanQueueEnabled", false) &&
            !string.IsNullOrWhiteSpace(connectionString);

        if (_enabled)
        {
            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }
        else
        {
            _queueClient = null!;
        }
    }

    public bool IsEnabled => _enabled;

    public async Task PublishAsync(DocumentScanMessage message, CancellationToken cancellationToken = default)
    {
        if (!_enabled)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(message);
        await _queueClient.SendMessageAsync(payload, cancellationToken);
    }
}
