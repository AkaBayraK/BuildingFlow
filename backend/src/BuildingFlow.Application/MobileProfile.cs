namespace BuildingFlow.Application;
public sealed record MobileProfileCommand(Guid OperationId,long ExpectedVersion,string FirstName,string LastName,string? Phone,string PhotoAction,string? PhotoBase64);
public sealed record MobileProfileDto(string UserId,string FirstName,string LastName,string? Phone,Guid? PhotoId,string? PhotoSha256,long Version,DateTimeOffset? ModifiedAt);
public sealed record MobileProfileResult(Guid OperationId,MobileProfileDto Profile,bool Replayed);
public interface IMobileProfileStore {
    Task<MobileProfileDto> ReadAsync(CancellationToken token);
    Task<MobileProfileResult> SaveAsync(MobileProfileCommand command,CancellationToken token);
    Task<byte[]?> PhotoAsync(Guid photo,CancellationToken token);
    // İŞ19: the centre shows account photos. Callers authorise the account first (user scope or person scope).
    Task<IReadOnlyDictionary<string,Guid>> CurrentPhotosAsync(IReadOnlyCollection<string> users,CancellationToken token);
    Task<byte[]?> AccountPhotoAsync(string user,CancellationToken token);
    Task MaintainAsync(CancellationToken token);
}
public static class ProfilePhoto {
    public const int MaxBytes=256*1024;
    // Structural JPEG validation + EXIF/APP/comment removal; no external URLs or executable image formats.
    public static byte[] Normalize(byte[] bytes, int maxBytes=MaxBytes, int maxDimension=1024) {
        if(bytes.Length < 4 || bytes.Length > maxBytes || bytes[0]!=255 || bytes[1]!=216) throw new ArgumentException("Invalid profile image.");
        using var output=new MemoryStream();output.Write(bytes,0,2);
        var i=2;var frame=false;var scan=false;
        while(i<bytes.Length) {
            if(bytes[i]!=255)throw new ArgumentException("Invalid JPEG marker.");
            var start=i++;while(i<bytes.Length && bytes[i]==255)i++;
            if(i>=bytes.Length)throw new ArgumentException("Truncated JPEG.");
            var marker=bytes[i++];
            if(marker==217) {
                if(!frame || !scan || i!=bytes.Length)throw new ArgumentException("Incomplete JPEG.");
                output.Write(bytes,start,i-start);return output.ToArray();
            }
            if(marker is 0 or 216 || marker is >=208 and <=215 || i+2>bytes.Length)throw new ArgumentException("Invalid JPEG segment.");
            var length=(bytes[i]<<8)|bytes[i+1];var end=i+length;
            if(length<2 || end>bytes.Length)throw new ArgumentException("Truncated JPEG segment.");
            if(marker is >=192 and <=207 && marker is not (196 or 200 or 204)) {
                if(marker is not (192 or 194) || length<8 || bytes[i+2]!=8 || frame)throw new ArgumentException("Unsupported JPEG frame.");
                var h=(bytes[i+3]<<8)|bytes[i+4];var w=(bytes[i+5]<<8)|bytes[i+6];
                if(w <1 || w >maxDimension || h <1 || h >maxDimension)throw new ArgumentException("JPEG dimensions exceed bounds.");
                frame=true;
            }
            if(marker==218) {
                if(!frame || length<6)throw new ArgumentException("Invalid JPEG scan.");
                scan=true;output.Write(bytes,start,end-start);i=end;
                var entropy=i;
                while(i<bytes.Length) {
                    if(bytes[i]!=255){i++;continue;}
                    var next=i+1;while(next<bytes.Length && bytes[next]==255)next++;
                    if(next>=bytes.Length)throw new ArgumentException("Truncated JPEG scan.");
                    if(bytes[next]==0 || bytes[next] is >=208 and <=215){i=next+1;continue;}
                    break;
                }
                output.Write(bytes,entropy,i-entropy);continue;
            }
            if(marker is not (>=224 and <=239) && marker!=254)output.Write(bytes,start,end-start);
            i=end;
        }
        throw new ArgumentException("JPEG missing end marker.");
    }
}
