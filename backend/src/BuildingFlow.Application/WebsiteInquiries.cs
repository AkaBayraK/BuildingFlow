namespace BuildingFlow.Application;
public sealed record InquiryRequest(Guid? ListingId,string FullName,string? Phone,string? Email,string Message,string PrivacyNoticeVersion,string Challenge,string? Website);
public sealed record InquiryChallenge(string Token,string PrivacyNoticeVersion,string PrivacyPath);
public sealed record InquiryUpdate(long Version,string Status);
public sealed record InquiryDto(Guid Id,Guid? ListingId,string FullName,string? Phone,string? Email,string Message,string PrivacyNoticeVersion,string Status,DateTimeOffset RetainUntil,long Version);
public interface IPublicWebsiteInquiryStore{
 Task<InquiryChallenge?> Challenge(string key,string ip,CancellationToken t);
 Task<bool> Submit(string key,string ip,InquiryRequest r,CancellationToken t);
}
public interface IWebsiteInquiryStore{
 Task<IReadOnlyList<InquiryDto>?> List(Guid property,AccessScope access,CancellationToken t);
 Task<InquiryDto?> Update(Guid property,Guid id,InquiryUpdate r,AccessScope access,CancellationToken t);
 Task Retain(CancellationToken t);
}

public sealed class InquiryRateLimitException:Exception{}
