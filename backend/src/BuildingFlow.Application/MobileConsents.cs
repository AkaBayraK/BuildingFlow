namespace BuildingFlow.Application;

// The mobile surface shows only the current state and the text to approve; the approval history stays with the centre (İŞ16 decision).
public sealed record MobileConsentText(Guid Id, int TextVersion, string Title, string Body, string BodySha256, bool RequiresReconsent, DateTimeOffset PublishedAt);
public sealed record MobileConsentState(string TypeCode, string NameTr, string NameEn, string Kind, string? Feature, int SortOrder, string State,
    int? ApprovedVersion, DateTimeOffset? ApprovedAt, Guid? CurrentTextId, int? CurrentVersion);
public sealed record MobileConsentItem(MobileConsentState Status, MobileConsentText? CurrentText);
public sealed record MobileConsentList(string UserId, MobileConsentItem[] Items);
public sealed record MobileConsentCommand(Guid OperationId, string? Action, string? TypeCode, Guid? TextId = null, string? BodySha256 = null, string? AppVersion = null);
public sealed record MobileConsentResult(Guid OperationId, MobileConsentState Status, bool Replayed);

public static class MobileConsentActions
{
    public const string Approve = "APPROVE", Withdraw = "WITHDRAW";
}

public interface IMobileConsentStore
{
    Task<MobileConsentList> ListAsync(CancellationToken token);
    // The same decision is recorded from the mobile app (MOBILE) or the centre's "Hesap bilgilerim" page (WEB).
    Task<MobileConsentResult> CommandAsync(MobileConsentCommand command, CancellationToken token, BuildingFlow.Domain.ConsentChannel channel = BuildingFlow.Domain.ConsentChannel.MOBILE);
}

// A coded 403: the caller is known and in scope, but a business precondition (such as consent) is missing.
public sealed class CodedForbiddenException(ApiErrorMessage error) : UnauthorizedAccessException(error.Message)
{
    public ApiErrorMessage Error { get; } = error;
}
