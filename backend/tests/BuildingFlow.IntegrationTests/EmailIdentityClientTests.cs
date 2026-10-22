using System.Net;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class EmailIdentityClientTests
{
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task SharedCreateAdapterAddsRequiredActionOnlyWhenEnabled(bool enabled)
    {
        var handler=new Handler();var client=Client(handler,enabled);
        Assert.Equal("target",await client.CreateAsync(new("owner","owner@example.test","Test","Owner",true),default));
        var body=JsonDocument.Parse(Assert.Single(handler.Writes)).RootElement;
        Assert.False(body.GetProperty("emailVerified").GetBoolean());
        if(enabled) Assert.Equal("VERIFY_EMAIL",body.GetProperty("requiredActions")[0].GetString());
        else Assert.False(body.TryGetProperty("requiredActions",out _));
    }

    [Fact] public async Task EnabledAdapterRejectsDifferentEmailAndOmitsSameEmailFromUpdate()
    {
        var handler=new Handler();var client=Client(handler,true);
        await Assert.ThrowsAsync<RequestValidationException>(()=>client.UpdateAsync("target",new("owner","new@example.test","Test","Owner",true),default));
        Assert.Empty(handler.Writes);
        await client.UpdateAsync("target",new("owner","old@example.test","Other","Name",true),default);
        var body=JsonDocument.Parse(Assert.Single(handler.Writes)).RootElement;
        Assert.False(body.TryGetProperty("email",out _));Assert.False(body.TryGetProperty("emailVerified",out _));
        Assert.Equal("Other",body.GetProperty("firstName").GetString());
    }

    [Fact] public async Task SmtpOrIdentityFailureIsNotReportedAsAcceptedAndContainsNoRawError()
    {
        var handler=new Handler{Fail=true};var client=Client(handler,true);
        var error=await Assert.ThrowsAsync<IdentityUnavailableException>(()=>client.SendActionsEmailAsync("target",["VERIFY_EMAIL"],default));
        Assert.DoesNotContain("private-marker",error.Message);
    }

    private static KeycloakAdminClient Client(Handler handler,bool enabled)=>new(new KeycloakAdminOptions{BaseUrl="http://identity.test",ClientSecret=new string('x',32),EmailVerificationEnabled=enabled},new HttpClient(handler),TimeProvider.System);
    private sealed class Handler : HttpMessageHandler
    {
        public List<string> Writes=[];public bool Fail;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var path=request.RequestUri!.AbsolutePath;
            if(path.EndsWith("/token"))return Json(new{access_token="fixture",expires_in=300});
            if(Fail)return new(HttpStatusCode.InternalServerError){Content=new StringContent("private-marker")};
            if(request.Method==HttpMethod.Get)return Json(new{id="target",username="owner",email="old@example.test",enabled=true,emailVerified=true});
            Writes.Add(await request.Content!.ReadAsStringAsync(token));
            var result=new HttpResponseMessage(request.Method==HttpMethod.Post?HttpStatusCode.Created:HttpStatusCode.NoContent);
            if(request.Method==HttpMethod.Post)result.Headers.Location=new Uri("http://identity.test/users/target");return result;
        }
        private static HttpResponseMessage Json(object body)=>new(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(body),Encoding.UTF8,"application/json")};
    }
}
