namespace BuildingFlow.Application;
public sealed record CommonAreaPhotoRequest(Guid DocumentId, long AreaVersion, bool RightsConfirmed);
public sealed record CommonAreaPhotoResponse(Guid Id, Guid PropertyId, Guid CommonAreaId, string FileName, string ContentType, long Size, string Sha256, long Version);
public interface ICommonAreaPhotoStore
{
    Task<IReadOnlyList<CommonAreaPhotoResponse>?> Photos(Guid property, Guid area, ActiveContextQuery? context, CancellationToken token);
    Task<CommonAreaPhotoResponse?> Link(Guid property, Guid area, CommonAreaPhotoRequest request, CancellationToken token);
    Task<bool> Unlink(Guid property, Guid area, Guid photo, long version, CancellationToken token);
    Task<DocumentDownload?> DownloadPhoto(Guid property, Guid area, Guid photo, ActiveContextQuery? context, CancellationToken token);
}
