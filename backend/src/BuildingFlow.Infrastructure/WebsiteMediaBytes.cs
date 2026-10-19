using BuildingFlow.Application;
using BuildingFlow.Domain;
namespace BuildingFlow.Infrastructure;
internal static class WebsiteMediaBytes{
 internal static async Task<WebsiteMediaContent?> Read(IFileStorage storage,WebsiteMedia m,CancellationToken t){using var source=await storage.DownloadAsync(m.ObjectKey,t);using var output=new MemoryStream();var buffer=new byte[81920];int count;while((count=await source.ReadAsync(buffer,t))>0){if(output.Length+count>m.Size)throw new IOException("Media integrity failed.");await output.WriteAsync(buffer.AsMemory(0,count),t);}var bytes=output.ToArray();if(bytes.Length!=m.Size||WebsiteImage.Sha(bytes)!=m.Sha256)throw new IOException("Media integrity failed.");return new(bytes,m.ContentType,m.Sha256);}
}
