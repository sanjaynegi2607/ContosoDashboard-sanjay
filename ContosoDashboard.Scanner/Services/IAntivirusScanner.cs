namespace ContosoDashboard.Scanner.Services;

public enum ScanVerdict
{
    Clean,
    MalwareDetected,
    TransientFailure
}

public sealed record ScanResult(ScanVerdict Verdict, string? UserSafeMessage = null);

public interface IAntivirusScanner
{
    Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default);
}
