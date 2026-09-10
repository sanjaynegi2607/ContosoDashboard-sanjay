namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task<string> ComputeSha256Async(Stream fileStream);
    Task DeleteAsync(string storedFilePath);
    Task<Stream> DownloadAsync(string storedFilePath);
}
