using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class PersonMessageConfiguration:IEntityTypeConfiguration<PersonMessage>
{
    public void Configure(EntityTypeBuilder<PersonMessage> b)
    {
        BaseEntityConfiguration.Configure(b,"person_messages");
        b.Property(v=>v.PropertyId).HasColumnName("property_id");b.Property(v=>v.PersonId).HasColumnName("person_id");b.Property(v=>v.RequestId).HasColumnName("request_id");
        b.Property(v=>v.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);b.Property(v=>v.RecipientUserId).HasColumnName("recipient_user_id").HasMaxLength(200);
        b.Property(v=>v.Title).HasColumnName("title").HasMaxLength(200);b.Property(v=>v.Body).HasColumnName("body").HasMaxLength(2000);b.Property(v=>v.ReadAt).HasColumnName("read_at");
        b.HasOne<Person>().WithMany().HasForeignKey(v=>new{v.PropertyId,v.PersonId}).HasPrincipalKey(v=>new{v.PropertyId,v.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(v=>new{v.PropertyId,v.RequestId}).IsUnique();b.HasIndex(v=>new{v.RecipientUserId,v.CreatedAt});
    }
}
public sealed class PersonMessageStore(BuildingFlowDbContext db,TimeProvider clock):IPersonMessageStore
{
    private IQueryable<Property> Properties(AccessScope a)=>db.Properties.Where(p=>!p.IsDeleted&&(a.ReadAll||db.PropertyMemberships.Any(m=>!m.IsDeleted&&m.IsEnabled&&m.PropertyId==p.Id&&m.UserId==a.UserId)));
    internal static IQueryable<PersonMessage> InboxQuery(BuildingFlowDbContext db,string user)=>db.PersonMessages.Where(v=>v.RecipientUserId==user&&db.Properties.Any(p=>p.Id==v.PropertyId&&p.IsEnabled)&&db.PropertyMemberships.Any(m=>m.PropertyId==v.PropertyId&&m.UserId==user&&m.IsEnabled));
    private IQueryable<PersonMessage> Inbox(string user)=>InboxQuery(db,user);
    private static System.Linq.Expressions.Expression<Func<PersonMessage,PersonMessageResponse>> Projection=>v=>new(v.Id,v.PropertyId,v.PersonId,v.Title,v.Body,v.CreatedAt,v.ReadAt);
    private static PersonMessageResponse Response(PersonMessage v)=>new(v.Id,v.PropertyId,v.PersonId,v.Title,v.Body,v.CreatedAt,v.ReadAt);
    public async Task<PersonMessageResponse?> SendAsync(Guid property,Guid person,SendPersonMessageRequest request,AccessScope access,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(token);
        var parent=await Properties(access).SingleOrDefaultAsync(p=>p.Id==property,token);if(parent==null)return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE",token);
        var target=await db.People.SingleOrDefaultAsync(p=>p.Id==person&&p.PropertyId==property,token);if(target==null)return null;
        var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{person,request.Version,Title=request.Title!.Trim(),Body=request.Body!.Trim(),request.Channel}))));
        var old=await db.PersonMessages.IgnoreQueryFilters().SingleOrDefaultAsync(v=>v.PropertyId==property&&v.RequestId==request.RequestId,token);
        if(old!=null){if(old.IsDeleted||old.Fingerprint!=fingerprint||old.CreatedBy!=access.UserId)throw new HierarchyConflictException(ErrorCatalog.PersonMessageConflict.Create());return Response(old);}
        if(!parent.IsEnabled||!target.IsEnabled||target.Version!=request.Version)throw new HierarchyConflictException(ErrorCatalog.PersonMessageConflict.Create());
        if(target.LinkedUserId==null||!await db.PropertyMemberships.AnyAsync(m=>m.PropertyId==property&&m.UserId==target.LinkedUserId&&m.IsEnabled,token))throw new RequestValidationException(new(){["linkedUserId"]=[ErrorCatalog.PersonAccountInvalid.Create()]});
        var message=new PersonMessage(property,person,request.RequestId,fingerprint,target.LinkedUserId,request.Title!,request.Body!);db.PersonMessages.Add(message);
        db.Entry(parent).Property(p=>p.IsEnabled).IsModified=true;await db.SaveChangesAsync(token);await tx.CommitAsync(token);return Response(message);
    }
    public async Task<PagedRows<PersonMessageResponse>?> HistoryAsync(Guid property,Guid person,int page,AccessScope access,CancellationToken token)
    {
        if(!await Properties(access).AnyAsync(p=>p.Id==property,token)||!await db.People.AnyAsync(p=>p.Id==person&&p.PropertyId==property,token))return null;
        return await Page(db.PersonMessages.Where(v=>v.PropertyId==property&&v.PersonId==person),page,token);
    }
    private async Task<PagedRows<PersonMessageResponse>> Page(IQueryable<PersonMessage> q,int page,CancellationToken token)=>new(await q.OrderByDescending(v=>v.CreatedAt).ThenBy(v=>v.Id).Skip((page-1)*10).Take(10).Select(Projection).ToListAsync(token),page,10,await q.CountAsync(token));
    public Task<PagedRows<PersonMessageResponse>> InboxAsync(string user,int page,CancellationToken token)=>Page(Inbox(user),page,token);
    public async Task<bool> ReadAsync(Guid id,string user,CancellationToken token){var row=await Inbox(user).SingleOrDefaultAsync(v=>v.Id==id,token);if(row==null)return false;if(row.ReadAt==null){row.MarkRead(clock.GetUtcNow());await db.SaveChangesAsync(token);}return true;}
}
