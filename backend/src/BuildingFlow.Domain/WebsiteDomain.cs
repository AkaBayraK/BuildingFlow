using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
namespace BuildingFlow.Domain;
public sealed class WebsiteDomain:BaseEntity{
 private WebsiteDomain(){}
 public WebsiteDomain(Guid p,Guid site,Guid request,string host,string kind,string hash){PropertyId=p;SiteId=site;RequestId=request;HostName=Normalize(host);if(kind is not("CUSTOM" or "PLATFORM_SUBDOMAIN")||hash.Length!=64)throw new ArgumentException("Invalid domain.");Kind=kind;VerificationTokenHash=hash;}
 public Guid PropertyId{get;private set;}public Guid SiteId{get;private set;}public Guid RequestId{get;private set;}public string HostName{get;private set;}=null!;public string Kind{get;private set;}=null!;public string Status{get;private set;}="PENDING";public string VerificationTokenHash{get;private set;}=null!;public DateTimeOffset? VerifiedAt{get;private set;}public DateTimeOffset? LastCheckedAt{get;private set;}public DateTimeOffset? ApprovedAt{get;private set;}public DateTimeOffset? TlsCheckedAt{get;private set;}public bool IsPrimary{get;private set;}
 public static string Normalize(string host){if(string.IsNullOrWhiteSpace(host)||host!=host.Trim()||host.Any(char.IsControl)||host.IndexOfAny(['/',':','@','\\','?','#','%'])>=0)throw new ArgumentException("Invalid host.");var h=new IdnMapping{UseStd3AsciiRules=true}.GetAscii(host.TrimEnd('.')).ToLowerInvariant();if(h.Length>253||IPAddress.TryParse(h,out _)||!h.Contains('.')||h.EndsWith(".localhost")||h.EndsWith(".local")||h.EndsWith(".internal")||h.Split('.').Any(x=>!Regex.IsMatch(x,"^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$",RegexOptions.CultureInvariant)))throw new ArgumentException("Invalid host.");return h;}
 public void Approve(DateTimeOffset now){ApprovedAt=now;}
 public void Check(bool dns,bool tls,DateTimeOffset now){LastCheckedAt=now;TlsCheckedAt=tls?now:null;Status=dns&&tls&&(Kind!="CUSTOM"||ApprovedAt!=null)?"VERIFIED":"PENDING";VerifiedAt=Status=="VERIFIED"?now:null;if(Status!="VERIFIED")IsPrimary=false;}
 public void Rotate(string hash){VerificationTokenHash=hash;Status="PENDING";IsPrimary=false;VerifiedAt=null;LastCheckedAt=null;TlsCheckedAt=null;}
 public void Disable(){Status="DISABLED";IsPrimary=false;}
 public void Primary(bool value){if(value&&Status!="VERIFIED")throw new ArgumentException("Unverified domain.");IsPrimary=value;}
}
