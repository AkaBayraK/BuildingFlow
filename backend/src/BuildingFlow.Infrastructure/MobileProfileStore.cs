using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class MobileProfileStore(BuildingFlowDbContext db,IMobileSessionStore sessions,ICurrentUserService actor,IFileStorage storage,TimeProvider clock):IMobileProfileStore,IManagedUserPhotoStore {
 private string User()=>actor.IsAuthenticated && !string.IsNullOrWhiteSpace(actor.UserId)?actor.UserId:throw new UnauthorizedAccessException();
 private async Task Authorize(CancellationToken token) {
  // The profile is the caller's own account data, so accounts without a person link (İŞ15) may read and edit it too.
  var s=await sessions.ReadAsync(User(),actor.Username,new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"}.Where(actor.IsInRole).ToArray(),token);
  if(s.HasMore)throw new UnauthorizedAccessException();
 }
 private static HierarchyConflictException Conflict()=>new(ErrorCatalog.MobileSyncConflict.Create());
 private async Task<MobileProfileDto> Dto(MobileProfile? p,string user,CancellationToken token) {
  var photo=p?.PhotoId is Guid id?await db.Set<MobileProfilePhoto>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id && x.OwnerUserId==user && x.State=="CURRENT" && x.IsEnabled,token):null;
  // Profiles saved before the name rule are shown in the same form; the next save stores it.
  return new(user,PersonNames.Given(p?.FirstName)??"",PersonNames.Family(p?.LastName)??"",p?.Phone,photo?.Id,photo?.Sha256,p?.Version??0,p?.ModifiedAt);
 }
 public async Task<MobileProfileDto> ReadAsync(CancellationToken token) {
  await Authorize(token);var user=User();var p=await db.Set<MobileProfile>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x=>x.OwnerUserId==user,token);
  if(p!=null && (p.IsDeleted || !p.IsEnabled))throw new UnauthorizedAccessException();
  return await Dto(p,user,token);
 }
 public Task<MobileProfileResult> SaveAsync(MobileProfileCommand c,CancellationToken token) => SaveCore(c,User(),Authorize,false,token);
 public async Task<UserPhotoState> ReadPhotoAsync(string target,CancellationToken token) {
  var p=await db.Set<MobileProfile>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x=>x.OwnerUserId==target,token);
  if(p!=null && (p.IsDeleted || !p.IsEnabled))throw new UnauthorizedAccessException();
  var dto=await Dto(p,target,token);
  return new(target,dto.PhotoId,dto.PhotoSha256,dto.Version,await ConsentGate.ActiveAsync(db,target,ConsentFeature.PROFILE_PHOTO,token));
 }
 public Task<MobileProfileResult> SavePhotoAsync(string target,UserPhotoCommand c,string firstName,string lastName,Func<CancellationToken,Task> authorize,CancellationToken token) {
  if(c.PhotoAction is not ("REPLACE" or "REMOVE"))throw new ArgumentException("Invalid photo action.");
  return SaveCore(new(c.OperationId,c.ExpectedVersion,firstName,lastName,null,c.PhotoAction,c.PhotoBase64),target,authorize,true,token);
 }
 private async Task<MobileProfileResult> SaveCore(MobileProfileCommand c,string user,Func<CancellationToken,Task> authorize,bool photoOnly,CancellationToken token) {
  await authorize(token);
  if(c.OperationId==Guid.Empty || c.ExpectedVersion<0 || (!photoOnly && (string.IsNullOrWhiteSpace(c.FirstName) || c.FirstName.Trim().Length>80 || c.FirstName.Any(char.IsControl) || string.IsNullOrWhiteSpace(c.LastName) || c.LastName.Trim().Length>80 || c.LastName.Any(char.IsControl))) || c.PhotoAction is not ("KEEP" or "REPLACE" or "REMOVE") || (c.PhotoAction!="REPLACE" && c.PhotoBase64!=null))throw new ArgumentException("Invalid profile.");
  var phone=string.IsNullOrWhiteSpace(c.Phone)?null:c.Phone.Trim();
  if(phone!=null) {
   var digits=phone.Count(char.IsAsciiDigit);
   if(phone.Length>32 || digits is <7 or >15 || phone.Any(x=>!char.IsAsciiDigit(x) && !"+ -()".Contains(x)))throw new ArgumentException("Invalid phone.");
  }
  byte[]? bytes=null;
  if(c.PhotoAction=="REPLACE") {
   if(c.PhotoBase64==null || c.PhotoBase64.Length>350000)throw new ArgumentException("Invalid image.");
   try {bytes=ProfilePhoto.Normalize(Convert.FromBase64String(c.PhotoBase64));}catch(FormatException){throw new ArgumentException("Invalid image encoding.");}
  }
  var hash=bytes==null?null:Convert.ToHexString(SHA256.HashData(bytes));
  var canonical=c with{FirstName=PersonNames.Given(c.FirstName)??"",LastName=PersonNames.Family(c.LastName)??"",Phone=phone,PhotoBase64=hash};
  var payload=photoOnly?"managed-profile-photo:"+JsonSerializer.Serialize(new{Actor=User(),Target=user,Command=new{canonical.OperationId,canonical.ExpectedVersion,canonical.PhotoAction,canonical.PhotoBase64}}):"profile:"+JsonSerializer.Serialize(canonical);
  var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
  // Durable reservation before upload: failed/uncertain storage writes can be reclaimed later.
  MobileProfilePhoto? staged=null;
  // T17.2: a new photo needs an ACTIVE PROFILE_PHOTO consent; KEEP and REMOVE never do, so a person can always delete.
  if(bytes!=null && !await db.Set<MobileOperationReceipt>().AnyAsync(x=>x.OwnerUserId==user && x.OperationId==c.OperationId,token))await ConsentGate.RequireAsync(db,user,ConsentFeature.PROFILE_PHOTO,token);
  if(bytes!=null) {
   staged=await db.Set<MobileProfilePhoto>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.OwnerUserId==user && x.OperationId==c.OperationId,token);
   if(staged==null) {
    staged=new(user,c.OperationId,fingerprint,hash!,bytes.Length);db.Add(staged);
    try {await db.SaveChangesAsync(token);}
    catch(DbUpdateException e) when(e.InnerException is Npgsql.PostgresException pg && pg.SqlState=="23505") {
     db.Entry(staged).State=EntityState.Detached;
     staged=await db.Set<MobileProfilePhoto>().IgnoreQueryFilters().SingleAsync(x=>x.OwnerUserId==user && x.OperationId==c.OperationId,token);
    }
   }
   if(staged.Fingerprint!=fingerprint || staged.IsDeleted || !staged.IsEnabled)throw Conflict();
  }
  await using var tx=await db.Database.BeginTransactionAsync(token);
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"profile:"+user},0))",token);
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({user+":"+c.OperationId},0))",token);
  await authorize(token);
  var receipt=await db.Set<MobileOperationReceipt>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.OwnerUserId==user && x.OperationId==c.OperationId,token);
  if(receipt!=null) {
   if(receipt.IsDeleted || receipt.Fingerprint!=fingerprint)throw Conflict();
   return new(c.OperationId,JsonSerializer.Deserialize<MobileProfileDto>(receipt.Result)!,true);
  }
  var p=await db.Set<MobileProfile>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.OwnerUserId==user,token);
  if(p!=null && (p.IsDeleted || !p.IsEnabled))throw new UnauthorizedAccessException();
  if(bytes!=null){await ConsentGate.LockAsync(db,user,token);await ConsentGate.RequireAsync(db,user,ConsentFeature.PROFILE_PHOTO,token);}
  if((p?.Version??0)!=c.ExpectedVersion)throw Conflict();
  var creating=p==null;
  if(p==null){
   // Mobile version>0 profiles require names. Seed a first photo profile from the existing directory;
   // never overwrite existing owner-saved details or change the Keycloak account.
   if(photoOnly && (string.IsNullOrWhiteSpace(canonical.FirstName)||canonical.FirstName.Length>80||canonical.FirstName.Any(char.IsControl)||string.IsNullOrWhiteSpace(canonical.LastName)||canonical.LastName.Length>80||canonical.LastName.Any(char.IsControl)))throw new ArgumentException("Invalid profile names.");
   p=new(user);db.Add(p);
  }
  var photoId=p.PhotoId;
  if(c.PhotoAction!="KEEP") {
   if(p.PhotoId is Guid old) {
    var previous=await db.Set<MobileProfilePhoto>().SingleAsync(x=>x.Id==old && x.OwnerUserId==user,token);previous.Retire();
   }
   photoId=null;
   if(bytes!=null && staged!=null) {
    await db.Entry(staged).ReloadAsync(token);
    using var stream=new MemoryStream(bytes);
    await storage.UploadAsync(staged.ObjectKey,stream,"image/jpeg",token);
    staged.Current();photoId=staged.Id;
   }
  }
  if(photoOnly && !creating)p.SetPhoto(photoId);else p.Edit(canonical.FirstName,canonical.LastName,phone,photoId);
  if(db.Entry(p).State!=EntityState.Added)db.Entry(p).State=EntityState.Modified;
  await db.SaveChangesAsync(token);
  var result=await Dto(p,user,token);
  db.Add(new MobileOperationReceipt(user,c.OperationId,fingerprint,JsonSerializer.Serialize(result)));
  await db.SaveChangesAsync(token);await tx.CommitAsync(token);
  return new(c.OperationId,result,false);
 }
 public async Task<byte[]?> PhotoAsync(Guid id,CancellationToken token) {
  await Authorize(token);var user=User();
  return await Download(await Current(user).SingleOrDefaultAsync(x=>x.Id==id,token),token);
 }
 private IQueryable<MobileProfilePhoto> Current(string user)=>db.Set<MobileProfilePhoto>().AsNoTracking().Where(x=>x.OwnerUserId==user && x.State=="CURRENT" && x.IsEnabled && db.Set<MobileProfile>().Any(p=>p.OwnerUserId==user && p.PhotoId==x.Id && p.IsEnabled));
 public async Task<IReadOnlyDictionary<string,Guid>> CurrentPhotosAsync(IReadOnlyCollection<string> users,CancellationToken token) {
  if(users.Count==0)return new Dictionary<string,Guid>();
  return await db.Set<MobileProfile>().AsNoTracking().Where(p=>users.Contains(p.OwnerUserId) && p.IsEnabled && p.PhotoId!=null && db.Set<MobileProfilePhoto>().Any(x=>x.Id==p.PhotoId && x.OwnerUserId==p.OwnerUserId && x.State=="CURRENT" && x.IsEnabled))
   .ToDictionaryAsync(p=>p.OwnerUserId,p=>p.PhotoId!.Value,StringComparer.Ordinal,token);
 }
 public async Task<byte[]?> AccountPhotoAsync(string user,CancellationToken token)=>string.IsNullOrWhiteSpace(user)?null:await Download(await Current(user).SingleOrDefaultAsync(token),token);
 private async Task<byte[]?> Download(MobileProfilePhoto? photo,CancellationToken token) {
  if(photo==null)return null;
  await using var stream=await storage.DownloadAsync(photo.ObjectKey,token);
  using var output=new MemoryStream();var buffer=new byte[8192];int read;
  while((read=await stream.ReadAsync(buffer,token))>0){if(output.Length+read>ProfilePhoto.MaxBytes)throw new StorageUnavailableException();await output.WriteAsync(buffer.AsMemory(0,read),token);}
  var bytes=output.ToArray();
  if(bytes.Length!=photo.Size || Convert.ToHexString(SHA256.HashData(bytes))!=photo.Sha256)throw new StorageUnavailableException();
  return bytes;
 }
 public async Task MaintainAsync(CancellationToken token) {
  var expiry=clock.GetUtcNow().AddMinutes(-30);
  var candidates=await db.Set<MobileProfilePhoto>().Where(x=>x.State=="RETIRED" || (x.State=="STAGED" && x.CreatedAt<expiry)).OrderBy(x=>x.CreatedAt).Take(5).ToArrayAsync(token);
  foreach(var photo in candidates) {
   await using var tx=await db.Database.BeginTransactionAsync(token);
   await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"profile:"+photo.OwnerUserId},0))",token);
   await db.Entry(photo).ReloadAsync(token);
   if(photo.State is not ("STAGED" or "RETIRED") || (photo.State=="STAGED" && photo.CreatedAt>=expiry) || await db.Set<MobileProfile>().IgnoreQueryFilters().AnyAsync(x=>x.PhotoId==photo.Id,token))continue;
   await storage.DeleteAsync(photo.ObjectKey,token);photo.Purged();await db.SaveChangesAsync(token);await tx.CommitAsync(token);
  }
 }
}
