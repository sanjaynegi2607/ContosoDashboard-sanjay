namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;

    public LocalFileStorageService()
    {
        _storageRoot = Path.Combine(AppContext.BaseDirectory, "AppData", "uploads");
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        if (fileStream == null)
            throw new ArgumentNullException(nameof(fileStream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        var extension = Path.GetExtension(fileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
        var safeFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var targetPath = Path.Combine(_storageRoot, safeFileName);

        await using var outputStream = File.Create(targetPath);
        await fileStream.CopyToAsync(outputStream);

        return targetPath;
    }

    public Task DeleteAsync(string storedFilePath)
    {
        if (!string.IsNullOrWhiteSpace(storedFilePath) && File.Exists(storedFilePath))
        {
            File.Delete(storedFilePath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string storedFilePath)
    {
        if (string.IsNullOrWhiteSpace(storedFilePath) || !File.Exists(storedFilePath))
        {
            throw new FileNotFoundException("Document file was not found.");
        }

        var stream = File.OpenRead(storedFilePath);
        return Task.FromResult<Stream>(stream);
    }
}
