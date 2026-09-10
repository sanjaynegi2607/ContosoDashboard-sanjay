namespace ContosoDashboard.Scanner.Services;

public sealed class DeterministicAntivirusScanner : IAntivirusScanner
{
    public Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ScanResult(ScanVerdict.Clean));
    }
}
