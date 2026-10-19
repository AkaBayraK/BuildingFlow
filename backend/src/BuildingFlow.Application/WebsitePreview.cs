namespace BuildingFlow.Application;
public sealed record WebsitePreviewIssue(string Token,DateTimeOffset ExpiresAt);
public sealed record WebsitePreviewRequest(string Token);
public interface IWebsitePreviewStore{
 Task<WebsitePreviewIssue?> Issue(Guid property,long version,AccessScope access,CancellationToken t);
 Task<PublicWebsite?> Read(string token,CancellationToken t);
 Task<WebsiteMediaContent?> Media(string token,Guid id,string sha,CancellationToken t);
}
