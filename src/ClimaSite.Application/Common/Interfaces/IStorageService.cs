namespace ClimaSite.Application.Common.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Uploads a file to storage and returns the URL
    /// </summary>
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string bucket = "products",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    Task<bool> DeleteAsync(
        string fileName,
        string bucket = "products",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a presigned URL for downloading a file
    /// </summary>
    Task<string> GetPresignedUrlAsync(
        string fileName,
        string bucket = "products",
        int expiryInSeconds = 3600,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage
    /// </summary>
    Task<bool> ExistsAsync(
        string fileName,
        string bucket = "products",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the bucket exists, creating it if necessary
    /// </summary>
    Task EnsureBucketExistsAsync(
        string bucket,
        CancellationToken cancellationToken = default);
}
