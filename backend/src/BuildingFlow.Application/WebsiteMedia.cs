using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;
public sealed record WebsiteMediaEdit(string AltText,string RightsBasis,string? RightsNote,bool ContainsPeople,bool PeopleConsentConfirmed,long Version);
public sealed record WebsiteMediaUpload(Guid RequestId,byte[] Bytes,string ContentType,string FileName,WebsiteMediaEdit Details);
public sealed record WebsiteMediaDto(Guid Id,Guid SiteId,string ContentType,int Size,int Width,int Height,string Sha256,string AltText,string RightsBasis,string? RightsNote,bool ContainsPeople,bool PeopleConsentConfirmed,long Version,bool Replayed=false);
public sealed record WebsiteMediaContent(byte[] Bytes,string ContentType,string Sha256);
public sealed record PublicWebsiteMedia(Guid Id,string ContentType,int Width,int Height,string Sha256,string AltText);
public interface IWebsiteMediaStore{
 Task<IReadOnlyList<WebsiteMediaDto>?> List(Guid property,AccessScope access,CancellationToken token);
 Task<WebsiteMediaDto?> Upload(Guid property,WebsiteMediaUpload request,byte[] clean,int width,int height,AccessScope access,CancellationToken token);
 Task<WebsiteMediaDto?> Edit(Guid property,Guid id,WebsiteMediaEdit request,AccessScope access,CancellationToken token);
 Task<bool> Delete(Guid property,Guid id,long version,AccessScope access,CancellationToken token);
 Task<WebsiteMediaContent?> Content(Guid property,Guid id,AccessScope access,CancellationToken token);

}
public interface IPublicWebsiteMediaStore {Task<WebsiteMediaContent?> Public(string key,Guid id,string sha,CancellationToken token);}
public sealed class WebsiteMediaService(IWebsiteMediaStore store,ICurrentUserService user){
 private AccessScope Access(){if(!user.IsAuthenticated||string.IsNullOrEmpty(user.UserId)||!new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER"}.Any(user.IsInRole))throw new UnauthorizedAccessException();return new(user.UserId,user.IsInRole("SUPER_ADMIN"));}
 private static void Validate(WebsiteMediaEdit r){try{_=new WebsiteMedia(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),new string('a',64),"image/jpeg",1,1,1,new string('a',64),r.AltText,r.RightsBasis,r.RightsNote,r.ContainsPeople,r.PeopleConsentConfirmed);}catch(ArgumentException){Invalid();}}
 internal static void Invalid()=>throw new RequestValidationException(new(){["media"]=[ErrorCatalog.WebsiteMediaInvalid.Create()]});
 public Task<IReadOnlyList<WebsiteMediaDto>?> List(Guid p,CancellationToken t)=>store.List(p,Access(),t);
 public Task<WebsiteMediaDto?> Upload(Guid p,WebsiteMediaUpload r,CancellationToken t){var access=Access();Validate(r.Details);if(r.RequestId==Guid.Empty)Invalid();byte[] clean=[];int w=0,h=0;try{(clean,w,h)=WebsiteImage.Normalize(r.Bytes,r.ContentType,r.FileName);}catch(ArgumentException){Invalid();}r=r with {Details=r.Details with {AltText=r.Details.AltText.Trim(),RightsNote=string.IsNullOrWhiteSpace(r.Details.RightsNote)?null:r.Details.RightsNote.Trim()}};return store.Upload(p,r,clean,w,h,access,t);}
 public Task<WebsiteMediaDto?> Edit(Guid p,Guid id,WebsiteMediaEdit r,CancellationToken t){var access=Access();Validate(r);if(r.Version<1)Invalid();return store.Edit(p,id,r,access,t);}
 public Task<bool> Delete(Guid p,Guid id,long v,CancellationToken t){if(v<1)Invalid();return store.Delete(p,id,v,Access(),t);}
 public Task<WebsiteMediaContent?> Content(Guid p,Guid id,CancellationToken t)=>store.Content(p,id,Access(),t);

}
public static class WebsiteImage{
 public const int MaxBytes=5*1024*1024;
 public static (byte[] Bytes,int Width,int Height) Normalize(byte[] bytes,string mime,string file){
  if(bytes.Length<4||bytes.Length>MaxBytes)throw new ArgumentException("Invalid image size.");
  var ext=Path.GetExtension(file).ToLowerInvariant();
  if(mime=="image/jpeg"&&ext is ".jpg" or ".jpeg"){
   var clean=ProfilePhoto.Normalize(bytes,MaxBytes,4096);int i=2;while(i+8<clean.Length){while(clean[i]==255)i++;var marker=clean[i++];var len=(clean[i]<<8)|clean[i+1];if(marker is 192 or 194)return(clean,(clean[i+5]<<8)|clean[i+6],(clean[i+3]<<8)|clean[i+4]);i+=len;}throw new ArgumentException("Missing dimensions.");
  }
  if(mime!="image/png"||ext!=".png"||!bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))throw new ArgumentException("Invalid image signature.");
  using var output=new MemoryStream();output.Write(bytes,0,8);using var pixels=new MemoryStream();int pos=8,width=0,height=0;bool header=false,data=false,ended=false,dataEnded=false;byte depth=0,color=0;var palette=false;
  while(pos<bytes.Length){if(pos+12>bytes.Length)throw new ArgumentException("Truncated PNG.");var len=(int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(pos,4));if(len<0||len>MaxBytes||pos+12L+len>bytes.Length)throw new ArgumentException("Invalid PNG chunk.");var type=System.Text.Encoding.ASCII.GetString(bytes,pos+4,4);var chunk=bytes.AsSpan(pos+4,len+4);uint crc=0xffffffff;foreach(var b in chunk){crc^=b;for(var j=0;j<8;j++)crc=(crc&1)!=0?(crc>>1)^0xedb88320:crc>>1;}if((crc^0xffffffff)!=BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(pos+8+len,4)))throw new ArgumentException("PNG CRC mismatch.");
   if(!header&&type!="IHDR")throw new ArgumentException("PNG needs header.");
   if(type=="IHDR"){if(header||len!=13)throw new ArgumentException("Invalid PNG header.");width=(int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(pos+8,4));height=(int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(pos+12,4));depth=bytes[pos+16];color=bytes[pos+17];if(width is <1 or >4096||height is <1 or >4096||depth!=8||color is not(0 or 2 or 3 or 4 or 6)||bytes[pos+18]!=0||bytes[pos+19]!=0||bytes[pos+20]!=0)throw new ArgumentException("Unsupported PNG.");header=true;}
   else if(type=="PLTE"){if(data||palette||len is <3 or >768||len%3!=0)throw new ArgumentException("Invalid PNG palette.");palette=true;}
   else if(type=="IDAT"){if(dataEnded||color==3&&!palette)throw new ArgumentException("Invalid PNG data.");pixels.Write(bytes,pos+8,len);data=true;}
   else if(type=="IEND"){if(!data||len!=0||pos+12!=bytes.Length)throw new ArgumentException("Invalid PNG end.");ended=true;}
   else if(type=="tRNS"){if(data||color is not(0 or 2 or 3)||len>256)throw new ArgumentException("Invalid transparency.");}
   else if((bytes[pos+4]&32)==0)throw new ArgumentException("Unknown critical PNG chunk.");
   if(data&&type!="IDAT"&&type!="IEND")dataEnded=true;
   // Drop all ancillary metadata (EXIF, text, ICC) except pixel transparency.
   if(type is "IHDR" or "PLTE" or "IDAT" or "IEND" or "tRNS")output.Write(bytes,pos,len+12);pos+=len+12;
  }
  if(!ended)throw new ArgumentException("PNG missing end.");
  pixels.Position=0;try{using var z=new ZLibStream(pixels,CompressionMode.Decompress);var channels=color switch{0=>1,2=>3,3=>1,4=>2,_=>4};var row=new byte[checked(width*channels+1)];for(int y=0;y<height;y++){z.ReadExactly(row);if(row[0]>4)throw new ArgumentException("Invalid PNG filter.");}if(z.ReadByte()!=-1)throw new ArgumentException("PNG pixels exceed bounds.");}catch(InvalidDataException){throw new ArgumentException("Invalid PNG pixels.");}catch(EndOfStreamException){throw new ArgumentException("Truncated PNG pixels.");}
  return(output.ToArray(),width,height);
 }
 public static string Sha(byte[] b)=>Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
}
public static class WebsiteMediaReferences{
 public static IReadOnlySet<Guid> Read(string json){var ids=new HashSet<Guid>();using var doc=JsonDocument.Parse(json);Visit(doc.RootElement,ids);return ids;}
 private static void Visit(JsonElement node,HashSet<Guid> ids){if(node.ValueKind==JsonValueKind.Object)foreach(var p in node.EnumerateObject()){if(p.Name is "mediaId" or "logoMediaId" or "heroMediaId" && p.Value.ValueKind==JsonValueKind.String && Guid.TryParse(p.Value.GetString(),out var id))ids.Add(id);else Visit(p.Value,ids);}else if(node.ValueKind==JsonValueKind.Array)foreach(var child in node.EnumerateArray())Visit(child,ids);}
 public static bool Contains(string json,Guid id)=>Read(json).Contains(id);
}
