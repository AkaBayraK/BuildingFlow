using System.Text;
using System.Xml;
using System.Xml.Linq;
using BuildingFlow.Application;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;

public sealed class PhoneOtpOptions
{
    public bool Enabled {get;init;}
    public string Usercode {get;init;}="";
    public string Password {get;init;}="";
    public string Sender {get;init;}="";
    public string HmacKey {get;init;}="";
    public bool Configured=>Enabled && !string.IsNullOrWhiteSpace(Usercode) && !string.IsNullOrWhiteSpace(Password) && Sender.Length is >=3 and <=11 && !Sender.Any(char.IsControl);
    public static PhoneOtpOptions From(IConfiguration c)=>new(){Enabled=bool.TryParse(c["ContactVerification:PhoneEnabled"],out var e)&&e,
        Usercode=c["ContactVerification:Netgsm:Usercode"]??"",Password=c["ContactVerification:Netgsm:Password"]??"",Sender=c["ContactVerification:Netgsm:Sender"]??"",HmacKey=c["ContactVerification:OtpKey"]??""};
}

// Dedicated platform OTP transport: never takes a Property account, template or credit balance.
public sealed class NetgsmPhoneOtpSender(PhoneOtpOptions options,HttpClient http) : IPhoneOtpSender
{
    public bool Available=>options.Configured;
    public async Task<PhoneOtpDelivery> SendAsync(string phone,string code,CancellationToken token)
    {
        if(!Available)return new("UNAVAILABLE");
        if(PhoneOtpPolicy.Normalize(phone)!=phone || !PhoneOtpPolicy.IsCode(code))throw new ArgumentException("Invalid OTP delivery.");
        var xml=new XDocument(new XElement("mainbody",new XElement("header",new XElement("usercode",options.Usercode),new XElement("password",options.Password),new XElement("msgheader",options.Sender)),
            new XElement("body",new XElement("msg",new XCData("BuildingFlow dogrulama kodunuz: "+code+". 5 dakika gecerlidir. Paylasmayin.")),new XElement("no",phone[3..]))));
        // No user-configurable URL, query credentials, redirect, retry or raw provider error logging.
        using var request=new HttpRequestMessage(HttpMethod.Post,"https://api.netgsm.com.tr/sms/send/otp"){Content=new StringContent(xml.ToString(SaveOptions.DisableFormatting),Encoding.UTF8,"application/xml")};
        using var budget=CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(http.Timeout==Timeout.InfiniteTimeSpan||http.Timeout>TimeSpan.FromSeconds(10)?TimeSpan.FromSeconds(10):http.Timeout);
        var deadline=budget.Token;
        try
        {
            using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline);
            if(!response.IsSuccessStatusCode)return new((int)response.StatusCode>=500 ? "UNKNOWN" : "FAILED");
            if(response.Content.Headers.ContentLength>8192)return new("UNKNOWN");
            await using var stream=await response.Content.ReadAsStreamAsync(deadline);
            var bytes=new byte[8193];var length=0;
            while(length<bytes.Length)
            {
                var read=await stream.ReadAsync(bytes.AsMemory(length),deadline);
                if(read==0)break;length+=read;
            }
            if(length>8192)return new("UNKNOWN");
            using var memory=new MemoryStream(bytes,0,length,false);
            using var reader=XmlReader.Create(memory,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=8192,Async=true});
            var body=await XDocument.LoadAsync(reader,LoadOptions.None,deadline);
            var main=body.Root?.Element("main");var result=main?.Element("code")?.Value;var id=main?.Element("jobID")?.Value;
            if(result=="0" && id is {Length:>0 and <=100} && id.All(char.IsAsciiDigit))return new("ACCEPTED",id);
            return new(result is "20" or "30" or "40" or "41" or "50" or "52" or "60" or "70" ? "FAILED" : "UNKNOWN");
        }
        catch(Exception error) when(error is HttpRequestException or IOException or OperationCanceledException or XmlException){return new("UNKNOWN");}
    }
}
