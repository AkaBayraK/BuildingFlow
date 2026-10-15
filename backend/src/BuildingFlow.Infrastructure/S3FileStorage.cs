using System.Net;
using System.Xml;
using System.Xml.Linq;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;

// SeaweedFS is reached through its authenticated S3 gateway. Keys and credentials never leave the API.
public sealed class S3FileStorage(IConfiguration configuration) : IFileStorage, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IAmazonS3? client;
    private string Bucket => configuration["ObjectStorage:Bucket"] ?? "buildingflow-documents";
    public static AmazonS3Client CreateClient(string endpoint, string accessKey, string secretKey) {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(url.UserInfo) || !string.IsNullOrEmpty(url.Query) || !string.IsNullOrEmpty(url.Fragment) || url.AbsolutePath != "/")
            throw new StorageUnavailableException();
        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey)) throw new StorageUnavailableException();
        return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), new AmazonS3Config {
            ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1",
            Timeout = TimeSpan.FromSeconds(30), MaxErrorRetry = 1,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        });
    }
    public static bool IsPrivatePolicyResponse(GetBucketPolicyResponse response) {
        if (response.HttpStatusCode == HttpStatusCode.OK) return string.IsNullOrWhiteSpace(response.Policy);
        // SeaweedFS returns a standard XML404 body; GetBucketPolicy's SDK unmarshaller returns it as Policy.
        if (response.HttpStatusCode != HttpStatusCode.NotFound || response.Policy is null || response.Policy.Length > 4096) return false;
        try {
            using var reader = XmlReader.Create(new StringReader(response.Policy), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 4096 });
            var root = XDocument.Load(reader).Root;
            return root?.Name.LocalName == "Error" && root.Elements().SingleOrDefault(e => e.Name.LocalName == "Code")?.Value == "NoSuchBucketPolicy";
        } catch { return false; }
    }
    private async Task<IAmazonS3> Ready(CancellationToken token) {
        await gate.WaitAsync(token);
        try {
            client ??= CreateClient(configuration["ObjectStorage:Endpoint"] ?? throw new StorageUnavailableException(),
                configuration["ObjectStorage:AccessKey"] ?? "", configuration["ObjectStorage:SecretKey"] ?? "");
            try { await client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = Bucket }, token); }
            catch (AmazonS3Exception error) when (error.StatusCode == System.Net.HttpStatusCode.NotFound) {
                try { await client.PutBucketAsync(new PutBucketRequest { BucketName = Bucket }, token); }
                catch (AmazonS3Exception race) when (race.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists") { }
            }
            // Every access rechecks the dedicated bucket; public or unexpected policies fail closed.
            try {
                var policy = await client.GetBucketPolicyAsync(new GetBucketPolicyRequest { BucketName = Bucket }, token);
                if (!IsPrivatePolicyResponse(policy)) throw new StorageUnavailableException();
            } catch (AmazonS3Exception error) when (error.ErrorCode == "NoSuchBucketPolicy") { }
            return client;
        } finally { gate.Release(); }
    }
    public async Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken token) {
        try {
            if (!content.CanSeek || content.Length is < 1 or > StoredDocument.MaxBytes) throw new StorageUnavailableException();
            var api = await Ready(token);
            await api.PutObjectAsync(new PutObjectRequest { BucketName = Bucket, Key = objectKey, InputStream = content,
                ContentType = contentType, AutoCloseStream = false, UseChunkEncoding = false }, token);
            var metadata = await api.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = Bucket, Key = objectKey }, token);
            if (metadata.ContentLength != content.Length) throw new StorageUnavailableException();
        } catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { throw new StorageUnavailableException(); }
    }
    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken token) {
        var result = new MemoryStream();
        try {
            var api = await Ready(token);
            using var response = await api.GetObjectAsync(new GetObjectRequest { BucketName = Bucket, Key = objectKey }, token);
            if (response.ContentLength is < 1 or > StoredDocument.MaxBytes) throw new StorageUnavailableException();
            var buffer = new byte[81920]; int read;
            while ((read = await response.ResponseStream.ReadAsync(buffer, token)) > 0) {
                if (result.Length + read > StoredDocument.MaxBytes) throw new StorageUnavailableException();
                await result.WriteAsync(buffer.AsMemory(0, read), token);
            }
            if (result.Length != response.ContentLength) throw new StorageUnavailableException();
            result.Position = 0; return result;
        } catch (OperationCanceledException) when (token.IsCancellationRequested) { result.Dispose(); throw; }
        catch { result.Dispose(); throw new StorageUnavailableException(); }
    }
    public async Task DeleteAsync(string objectKey, CancellationToken token) {
        try { var api = await Ready(token); await api.DeleteObjectAsync(new DeleteObjectRequest { BucketName = Bucket, Key = objectKey }, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { throw new StorageUnavailableException(); }
    }
    public void Dispose() { client?.Dispose(); gate.Dispose(); }
}
