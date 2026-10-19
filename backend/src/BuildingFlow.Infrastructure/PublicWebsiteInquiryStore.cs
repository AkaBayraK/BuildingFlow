using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;
public sealed class WebsiteInquiryProtection(IConfiguration config,TimeProvider clock){
 private byte[] Key(){try{var k=Convert.FromBase64String(config["Website:InquiryHmacKey"]??"");if(k.Length>=32)return k;}catch(FormatException){}throw new HierarchyConflictException(ErrorCatalog.InquiryUnavailable.Create());}
 public string Hash(string value)=>Convert.ToHexString(HMACSHA256.HashData(Key(),Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
 public string Ip(string ip,int days=0)=>Hash(clock.GetUtcNow().AddDays(days).ToString("yyyy-MM-dd")+"|"+ip);
 public string Issue(string key,string ip,string version){var payload=Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[]{key,Ip(ip),version,clock.GetUtcNow().ToUnixTimeSeconds().ToString(),Guid.NewGuid().ToString()})));return payload+"."+Hash(payload);}
 public bool Check(string token,string key,string ip,string version){try{var parts=token.Split('.');if(parts.Length!=2||!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(parts[1]),Encoding.ASCII.GetBytes(Hash(parts[0]))))return false;var v=JsonSerializer.Deserialize<string[]>(Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])))!;var seconds=clock.GetUtcNow().ToUnixTimeSeconds()-long.Parse(v[3]);return v.Length==5&&v[0]==key&&v[1]==Ip(ip)&&v[2]==version&&seconds is >=3 and <=1800;}catch(Exception e)when(e is FormatException or JsonException or IndexOutOfRangeException){return false;}}
}
public sealed class PublicWebsiteInquiryStore(BuildingFlowDbContext db,TimeProvider clock,WebsiteInquiryProtection protection):IPublicWebsiteInquiryStore{
 private IQueryable<WebsiteSite> Public(string key)=>db.Set<WebsiteSite>().Where(s=>s.SiteKey==key&&s.IsEnabled&&s.Status==WebsiteStatus.PUBLISHED&&db.Properties.Any(p=>p.Id==s.PropertyId&&p.IsEnabled));
 private Task<WebsitePublication?> Publication(WebsiteSite s,CancellationToken t)=>db.Set<WebsitePublication>().SingleOrDefaultAsync(x=>x.PropertyId==s.PropertyId&&x.SiteId==s.Id&&x.IsActive&&x.IsEnabled,t);
 public async Task<InquiryChallenge?> Challenge(string key,string ip,CancellationToken t){var s=await Public(key).SingleOrDefaultAsync(t);if(s is null)return null;var pub=await Publication(s,t);if(pub is null)return null;using var doc=JsonDocument.Parse(pub.Snapshot);var privacy=doc.RootElement.GetProperty("pages").EnumerateArray().FirstOrDefault(x=>x.GetProperty("kind").GetString()=="PRIVACY");if(privacy.ValueKind==JsonValueKind.Undefined)return null;return new(protection.Issue(key,ip,pub.SnapshotSha256),pub.SnapshotSha256,"/"+key+"/"+privacy.GetProperty("slug").GetString());}
 public async Task<bool> Submit(string key,string ip,InquiryRequest r,CancellationToken t){
  if(!string.IsNullOrEmpty(r.Website))return true;
  if(string.IsNullOrWhiteSpace(r.FullName)||r.FullName.Length>100||string.IsNullOrWhiteSpace(r.Message)||r.Message.Length>2000||r.Phone?.Length>40||r.Email?.Length>200||(string.IsNullOrWhiteSpace(r.Phone)&&string.IsNullOrWhiteSpace(r.Email))||r.Email is not null&&!System.Net.Mail.MailAddress.TryCreate(r.Email,out _)||string.IsNullOrWhiteSpace(r.Challenge)||r.Challenge.Length>2000||!protection.Check(r.Challenge,key,ip,r.PrivacyNoticeVersion))Invalid();
  await using var tx=await db.Database.BeginTransactionAsync(t);var site=await Public(key).SingleOrDefaultAsync(t);if(site is null)return false;
  // Serialize global IP and site budgets across API instances, without persisting the raw IP.
  var hash=protection.Ip(ip);var previousHash=protection.Ip(ip,-1);var budget=protection.Hash("rate|"+ip);await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({budget},0))",t);await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={site.PropertyId} FOR UPDATE",t);
  if(!await Public(key).AnyAsync(t))return false;var pub=await Publication(site,t);if(pub is null||pub.SnapshotSha256!=r.PrivacyNoticeVersion)Invalid();var now=clock.GetUtcNow();var hour=now.AddHours(-1);var day=now.UtcDateTime.Date;
  if(await db.Set<WebsiteInquiry>().CountAsync(x=>(x.IpHash==hash||x.IpHash==previousHash)&&x.CreatedAt>hour,t)>=5||await db.Set<WebsiteInquiry>().CountAsync(x=>x.SiteId==site.Id&&x.CreatedAt>=new DateTimeOffset(day,TimeSpan.Zero),t)>=100)throw new InquiryRateLimitException();
  if(r.ListingId is not null&&!await db.Set<PropertyListing>().AnyAsync(x=>x.Id==r.ListingId&&x.PropertyId==site.PropertyId&&x.SiteId==site.Id&&x.IsEnabled&&x.Status=="PUBLISHED"&&x.PublishUntil>now,t))Invalid();
  db.Add(new WebsiteInquiry(site.PropertyId,site.Id,r.ListingId,r.FullName.Trim(),r.Phone,r.Email,r.Message.Trim(),r.PrivacyNoticeVersion,hash,now));await db.SaveChangesAsync(t);await tx.CommitAsync(t);return true;
 }
 private static void Invalid()=>throw new RequestValidationException(new(){["inquiry"]=[ErrorCatalog.InquiryRejected.Create()]});
}
