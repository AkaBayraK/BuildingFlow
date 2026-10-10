using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
namespace BuildingFlow.Infrastructure;
// Credentials and object keys remain server-side. A callback's remote stream never escapes its lifetime.
public sealed class MinioFileStorage(IConfiguration configuration) : IFileStorage, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IMinioClient? client;
    private string Bucket => configuration["MinIO:Bucket"] ?? "buildingflow-documents";
    private async Task<IMinioClient> Ready(CancellationToken token)
    {
        await gate.WaitAsync(token);
        try {
            client ??= new MinioClient().WithEndpoint(configuration["MinIO:Endpoint"] ?? "127.0.0.1:9000")
                .WithCredentials(configuration["MinIO:AccessKey"] ?? throw new StorageUnavailableException(), configuration["MinIO:SecretKey"] ?? throw new StorageUnavailableException())
                .WithSSL(string.Equals(configuration["MinIO:UseSSL"], "true", StringComparison.OrdinalIgnoreCase)).WithTimeout(30000).Build();
            if (!await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(Bucket), token))
                await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(Bucket), token);
            try {
                var policy = await client.GetPolicyAsync(new GetPolicyArgs().WithBucket(Bucket), token);
                // A dedicated bucket must have no public/access policy. Do not silently rewrite an operator's policy.
                if (!string.IsNullOrWhiteSpace(policy)) throw new StorageUnavailableException();
            } catch (MinioException error) when (error.Response?.Code == "NoSuchBucketPolicy") { }
            return client;
        } finally { gate.Release(); }
    }
    public async Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken token)
    {
        try {
            if (!content.CanSeek || content.Length is < 1 or > StoredDocument.MaxBytes) throw new StorageUnavailableException();
            var api = await Ready(token);
            await api.PutObjectAsync(new PutObjectArgs().WithBucket(Bucket).WithObject(objectKey).WithStreamData(content).WithObjectSize(content.Length).WithContentType(contentType), token);
            var stat = await api.StatObjectAsync(new StatObjectArgs().WithBucket(Bucket).WithObject(objectKey), token);
            if (stat.Size != content.Length) throw new StorageUnavailableException();
        } catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { throw new StorageUnavailableException(); }
    }
    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken token)
    {
        var result = new MemoryStream();
        try {
            var api = await Ready(token);
            await api.GetObjectAsync(new GetObjectArgs().WithBucket(Bucket).WithObject(objectKey).WithCallbackStream(async (source, ct) => {
                var buffer = new byte[81920]; int read;
                while ((read = await source.ReadAsync(buffer, ct)) > 0) {
                    if (result.Length + read > StoredDocument.MaxBytes) throw new StorageUnavailableException();
                    await result.WriteAsync(buffer.AsMemory(0, read), ct);
                }
            }), token);
            result.Position = 0; return result;
        } catch (OperationCanceledException) when (token.IsCancellationRequested) { result.Dispose(); throw; }
        catch { result.Dispose(); throw new StorageUnavailableException(); }
    }
    public async Task DeleteAsync(string objectKey, CancellationToken token)
    {
        try { var api = await Ready(token); await api.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(Bucket).WithObject(objectKey), token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { throw new StorageUnavailableException(); }
    }
    public void Dispose() { client?.Dispose(); gate.Dispose(); }
}
