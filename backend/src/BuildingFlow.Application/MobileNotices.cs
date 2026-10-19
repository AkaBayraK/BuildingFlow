namespace BuildingFlow.Application;
public sealed record MobileNoticeContext(Guid PropertyId,Guid? UnitId,string Role);
public sealed record MobileNoticeQuery(Guid PropertyId,Guid? UnitId,string Role,string Kind,int Page,Guid? ItemId=null);
public sealed record MobileNoticeItem(Guid Id,string Title,string Body,DateTimeOffset CreatedAt,DateTimeOffset? ReadAt);
public sealed record MobileNoticePreferenceDto(bool HighlightsEnabled,long Version);
public sealed record MobileNoticeSnapshot(string UserId,string ContextKey,DateTimeOffset AsOf,string PropertyName,string Kind,int Page,Guid? ItemId,int Total,int Unread,MobileNoticePreferenceDto Preference,bool PushAvailable,MobileNoticeItem[] Items);
public sealed record MobileNoticeReadCommand(Guid PropertyId,Guid? UnitId,string Role,string Kind,Guid Id);
public sealed record MobileNoticeReadResult(Guid Id,string Kind,DateTimeOffset ReadAt);
public sealed record MobileNoticePreferenceCommand(Guid PropertyId,Guid? UnitId,string Role,bool HighlightsEnabled,long ExpectedVersion);
public interface IMobileNoticeStore{
 Task<MobileNoticeSnapshot> ReadAsync(MobileNoticeQuery query,CancellationToken token);
 Task<MobileNoticeReadResult> MarkReadAsync(MobileNoticeReadCommand command,CancellationToken token);
 Task<MobileNoticePreferenceDto> PreferenceAsync(MobileNoticePreferenceCommand command,CancellationToken token);
}
