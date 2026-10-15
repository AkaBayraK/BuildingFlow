namespace BuildingFlow.Application;
public sealed record SendPersonMessageRequest(Guid RequestId,long Version,string? Title,string? Body,string Channel="IN_APP");
public sealed record PersonMessageResponse(Guid Id,Guid PropertyId,Guid PersonId,string Title,string Body,DateTimeOffset CreatedAt,DateTimeOffset? ReadAt);
public interface IPersonMessageStore
{
    Task<PersonMessageResponse?> SendAsync(Guid property,Guid person,SendPersonMessageRequest request,AccessScope access,CancellationToken token);
    Task<PagedRows<PersonMessageResponse>?> HistoryAsync(Guid property,Guid person,int page,AccessScope access,CancellationToken token);
    Task<PagedRows<PersonMessageResponse>> InboxAsync(string user,int page,CancellationToken token);
    Task<bool> ReadAsync(Guid id,string user,CancellationToken token);
}
public sealed class PersonMessageService(IPersonMessageStore store,ICurrentUserService user)
{
    private string User()=>user.IsAuthenticated && !string.IsNullOrWhiteSpace(user.UserId)?user.UserId:throw new UnauthorizedAccessException();
    private AccessScope Management(){var id=User();if(!(user.IsInRole("SUPER_ADMIN")||user.IsInRole("SITE_ADMIN")||user.IsInRole("MANAGER")))throw new UnauthorizedAccessException();return new(id,user.IsInRole("SUPER_ADMIN"));}
    private static void Page(int page){if(page is <1 or >1000000)throw new RequestValidationException(new(){["page"]=[ErrorCatalog.InvalidPage.Create()]});}
    public Task<PersonMessageResponse?> SendAsync(Guid property,Guid person,SendPersonMessageRequest request,CancellationToken token)
    {
        var access=Management();
        if(request.RequestId==Guid.Empty||request.Version<1||request.Channel!="IN_APP"||string.IsNullOrWhiteSpace(request.Title)||request.Title.Trim().Length>200||string.IsNullOrWhiteSpace(request.Body)||request.Body.Trim().Length>2000||request.Title.Any(char.IsControl)||request.Body.Any(c=>char.IsControl(c)&& c is not ('\r' or '\n' or '\t')))
            throw new RequestValidationException(new(){["body"]=[ErrorCatalog.PersonMessageInvalid.Create()]});
        return store.SendAsync(property,person,request,access,token);
    }
    public Task<PagedRows<PersonMessageResponse>?> HistoryAsync(Guid property,Guid person,int page,CancellationToken token){var access=Management();Page(page);return store.HistoryAsync(property,person,page,access,token);}
    public Task<PagedRows<PersonMessageResponse>> InboxAsync(int page,CancellationToken token){var id=User();Page(page);return store.InboxAsync(id,page,token);}
    public Task<bool> ReadAsync(Guid id,CancellationToken token)=>store.ReadAsync(id,User(),token);
}
