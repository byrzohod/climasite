using ClimaSite.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace ClimaSite.Infrastructure.Services;

public class MinioStorageSettings
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "climasite";
    public string SecretKey { get; set; } = "climasite_minio_secret";
    public bool UseSSL { get; set; } = false;
    public string PublicUrl { get; set; } = "http://localhost:9000";
}

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioStorageSettings _settings;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(
        IOptions<MinioStorageSettings> settings,
        ILogger<MinioStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _minioClient = new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey)
            .WithSSL(_settings.UseSSL)
            .Build();
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string bucket = "products",
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureBucketExistsAsync(bucket, cancellationToken);

            // Generate unique file name
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            var putArgs = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(uniqueFileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putArgs, cancellationToken);

            _logger.LogInformation("File {FileName} uploaded to bucket {Bucket}", uniqueFileName, bucket);

            // Return the public URL
            return $"{_settings.PublicUrl}/{bucket}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to bucket {Bucket}", fileName, bucket);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        string fileName,
        string bucket = "products",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName);

            await _minioClient.RemoveObjectAsync(removeArgs, cancellationToken);

            _logger.LogInformation("File {FileName} deleted from bucket {Bucket}", fileName, bucket);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileName} from bucket {Bucket}", fileName, bucket);
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(
        string fileName,
        string bucket = "products",
        int expiryInSeconds = 3600,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var presignedArgs = new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName)
                .WithExpiry(expiryInSeconds);

            var url = await _minioClient.PresignedGetObjectAsync(presignedArgs);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for {FileName} in bucket {Bucket}", fileName, bucket);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        string fileName,
        string bucket = "products",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName);

            await _minioClient.StatObjectAsync(statArgs, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task EnsureBucketExistsAsync(
        string bucket,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existsArgs = new BucketExistsArgs().WithBucket(bucket);
            var exists = await _minioClient.BucketExistsAsync(existsArgs, cancellationToken);

            if (!exists)
            {
                var makeArgs = new MakeBucketArgs().WithBucket(bucket);
                await _minioClient.MakeBucketAsync(makeArgs, cancellationToken);

                // Set bucket policy to allow public read access for product images
                var policy = $@"{{
                    ""Version"": ""2012-10-17"",
                    ""Statement"": [
                        {{
                            ""Effect"": ""Allow"",
                            ""Principal"": {{""AWS"": [""*""]}},
                            ""Action"": [""s3:GetObject""],
                            ""Resource"": [""arn:aws:s3:::{bucket}/*""]
                        }}
                    ]
                }}";

                var policyArgs = new SetPolicyArgs()
                    .WithBucket(bucket)
                    .WithPolicy(policy);

                await _minioClient.SetPolicyAsync(policyArgs, cancellationToken);

                _logger.LogInformation("Created bucket {Bucket} with public read policy", bucket);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring bucket {Bucket} exists", bucket);
            throw;
        }
    }
}
