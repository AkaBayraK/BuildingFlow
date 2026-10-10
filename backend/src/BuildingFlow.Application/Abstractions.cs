namespace BuildingFlow.Application;

public interface ICurrentUserService { string? UserId { get; } string? Username { get; } bool IsAuthenticated { get; } bool IsInRole(string role); }
public interface IFileStorage
{
    Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken);
    Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
// Only versioned IMessageCommand types can be published. Business writes need the transactional Outbox (phase09 step02).
public interface IMessagePublisher { Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class; }
