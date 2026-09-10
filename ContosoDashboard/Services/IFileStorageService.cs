namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteAsync(string storedFilePath);
    Task<Stream> DownloadAsync(string storedFilePath);
}
