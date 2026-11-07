using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BuildingFlow.Api;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Sesli yazışma: management roles only, server-side local STT, strict size/type checks; audio and transcript are not stored.
public sealed class AssistantVoiceTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Root = "/api/v1/assistant";
    private static readonly byte[] Wav = [.. "RIFF"u8, 36, 0, 0, 0, .. "WAVE"u8, .. "fmt "u8, 16, 0, 0, 0, 1, 0, 1, 0, 0x80, 0x3E, 0, 0, 0, 0x7D, 0, 0, 2, 0, 16, 0, .. "data"u8, 0, 0, 0, 0];
    private static readonly byte[] Webm = [0x1A, 0x45, 0xDF, 0xA3, 0x9F, 0x42, 0x86, 0x81, 0x01];

    private sealed class FakeStt : HttpMessageHandler
    {
        public string Text { get; set; } = "Bu ay geciken aidat ne kadar?";
        public bool Down { get; set; }
        public List<(string Path, string Language, string Type, int Bytes, bool HasModel)> Calls { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (Down) throw new HttpRequestException("refused");
            var form = (MultipartFormDataContent)request.Content!;
            string Field(string name) => form.Single(p => p.Headers.ContentDisposition!.Name!.Trim('"') == name) is var part ? part.ReadAsStringAsync(token).Result : "";
            var file = form.Single(p => p.Headers.ContentDisposition!.Name!.Trim('"') == "file");
            Calls.Add((request.RequestUri!.AbsolutePath, Field("language"), file.Headers.ContentType!.MediaType!, (await file.ReadAsByteArrayAsync(token)).Length, form.Any(p => p.Headers.ContentDisposition!.Name!.Trim('"') == "model")));
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { text = Text }), Encoding.UTF8, "application/json") };
        }
    }

    private WebApplicationFactory<Program> App(FakeStt? stt = null, string? path = null) => factory.WithWebHostBuilder(builder =>
    {
        if (stt is null) return;
        builder.UseSetting("Assistant:Speech:BaseUrl", "http://stt.test");
        if (path is not null) builder.UseSetting("Assistant:Speech:Path", path);
        builder.ConfigureServices(services => services.AddHttpClient(LocalSpeechTranscriber.ClientName).ConfigurePrimaryHttpMessageHandler(() => stt));
    });

    private async Task<(Guid Property, string Actor)> Seed()
    {
        var actor = Guid.NewGuid().ToString(); var p = new Property("Voice " + actor, "City", "District", "Address", PropertyType.APARTMENT);
        await factory.WithDb(async db => { db.AddRange(p, new PropertyMembership(p.Id, actor)); await db.SaveChangesAsync(); });
        return (p.Id, actor);
    }

    private static MultipartFormDataContent Audio(Guid property, byte[] bytes, string type = "audio/wav", int? duration = 4000, string? extra = null, string? locale = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes); file.Headers.ContentType = MediaTypeHeaderValue.Parse(type); form.Add(file, "audio", "speech.bin");
        form.Add(new StringContent(property.ToString()), "propertyId");
        if (duration is not null) form.Add(new StringContent(duration.Value.ToString()), "durationMs");
        if (extra is not null) form.Add(new StringContent("x"), extra);
        if (locale is not null) form.Add(new StringContent(locale), "locale");
        return form;
    }

    [PostgresFact] public async Task OnlyManagementRolesReachVoice()
    {
        var x = await Seed(); var app = App(new FakeStt());
        using var anon = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(Root + "/voice/status")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsync(Root + "/transcribe", Audio(x.Property, Wav))).StatusCode);
        foreach (var role in new[] { "ACCOUNTANT", "RESIDENT", "OWNER", "TENANT", "SECURITY" })
        {
            using var c = TestToken.Client(app, x.Actor, role);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync(Root + "/voice/status")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsync(Root + "/transcribe", Audio(x.Property, Wav))).StatusCode);
        }
        foreach (var role in new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" })
        {
            using var c = TestToken.Client(app, x.Actor, role);
            var status = await Json(await c.GetAsync(Root + "/voice/status"), HttpStatusCode.OK);
            Assert.True(status.GetProperty("enabled").GetBoolean()); Assert.Equal(60, status.GetProperty("maxSeconds").GetInt32());
        }
    }

    [PostgresFact] public async Task WithoutAProviderVoiceIsDisabledWithAReason()
    {
        var x = await Seed();
        using var c = TestToken.Client(App(), x.Actor, "MANAGER");
        var status = await Json(await c.GetAsync(Root + "/voice/status"), HttpStatusCode.OK);
        Assert.Equal((false, "PROVIDER_NOT_CONFIGURED"), (status.GetProperty("enabled").GetBoolean(), status.GetProperty("reason").GetString()));
        await Code(await c.PostAsync(Root + "/transcribe", Audio(x.Property, Wav)), HttpStatusCode.ServiceUnavailable, "BF_ASSISTANT_VOICE_UNAVAILABLE");
    }

    [PostgresFact] public async Task SizeTypeSignatureAndDurationAreEnforcedBeforeTheProvider()
    {
        var x = await Seed(); var stt = new FakeStt(); var app = App(stt);
        using var c = TestToken.Client(app, x.Actor, "MANAGER");
        foreach (var bad in new[] { Audio(x.Property, Wav, "text/plain"), Audio(x.Property, Wav, "audio/webm"), Audio(x.Property, Webm, "audio/wav"), Audio(x.Property, "hello"u8.ToArray(), "audio/ogg"),
                     Audio(x.Property, [.. Wav, .. new byte[10 * 1024 * 1024]]), Audio(x.Property, Wav, duration: 61000), Audio(x.Property, Wav, duration: 0), Audio(x.Property, Wav, duration: null), Audio(x.Property, Wav, extra: "transcript"), Audio(Guid.Empty, Wav) })
            await Code(await c.PostAsync(Root + "/transcribe", bad), HttpStatusCode.BadRequest, "BF_ASSISTANT_VOICE_INVALID");
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync(Root + "/transcribe", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync(Root + "/transcribe", Audio(Guid.NewGuid(), Wav))).StatusCode);
        Assert.Empty(stt.Calls);
        Assert.Equal(0, (await Json(await c.GetAsync(Root + "/status"), HttpStatusCode.OK)).GetProperty("quota").GetProperty("usedToday").GetInt32());
    }

    [PostgresFact] public async Task TranscriptFillsTheChatBoxAndNothingIsStored()
    {
        var x = await Seed(); var stt = new FakeStt { Text = "  Bu ay geciken\u0007 aidat ne kadar?  " }; var app = App(stt, "/inference");
        using var c = TestToken.Client(app, x.Actor, "SITE_ADMIN");
        var body = await Json(await c.PostAsync(Root + "/transcribe", Audio(x.Property, Webm, "audio/webm;codecs=opus")), HttpStatusCode.OK);
        Assert.Equal("Bu ay geciken aidat ne kadar?", body.GetProperty("text").GetString());
        Assert.Equal(1, body.GetProperty("quota").GetProperty("usedToday").GetInt32());
        var call = Assert.Single(stt.Calls); Assert.Equal(("/inference", "tr", "audio/webm", Webm.Length, false), call);
        await factory.WithDb(async db =>
        {
            var audit = await db.Set<AssistantAuditEntry>().SingleAsync(a => a.PropertyId == x.Property);
            Assert.Equal(("FREE_TEXT", "TRANSCRIBE", "SPEECH", "ANSWERED"), (audit.RequestKind, audit.ToolKey, audit.Source, audit.Outcome));
        });
        Assert.DoesNotContain(typeof(AssistantAuditEntry).GetProperties(), p => p.Name.Contains("Audio") || p.Name.Contains("Text") || p.Name.Contains("Transcript"));
        stt.Text = new string('a', 900);
        Assert.Equal(500, (await Json(await c.PostAsync(Root + "/transcribe", Audio(x.Property, Wav, locale: "en")), HttpStatusCode.OK)).GetProperty("text").GetString()!.Length);
        Assert.Equal("en", stt.Calls[^1].Language);
        stt.Down = true;
        await Code(await c.PostAsync(Root + "/transcribe", Audio(x.Property, Wav)), HttpStatusCode.ServiceUnavailable, "BF_ASSISTANT_VOICE_UNAVAILABLE");
    }

    [Fact] public void SignaturesMatchTheirDeclaredTypes()
    {
        Assert.True(AssistantVoiceService.Signature("audio/wav", Wav)); Assert.True(AssistantVoiceService.Signature("audio/webm", Webm));
        Assert.True(AssistantVoiceService.Signature("audio/ogg", "OggS\0\x02"u8)); Assert.True(AssistantVoiceService.Signature("audio/mp4", [0, 0, 0, 0x20, .. "ftypM4A "u8]));
        Assert.True(AssistantVoiceService.Signature("audio/mpeg", "ID3\x04"u8)); Assert.False(AssistantVoiceService.Signature("audio/mpeg", "RIFF"u8));
        Assert.Equal("audio/wav", AssistantVoiceService.BaseType("audio/x-wav")); Assert.Equal("audio/webm", AssistantVoiceService.BaseType("audio/webm; codecs=opus"));
        Assert.Null(AssistantVoiceService.BaseType("video/mp4")); Assert.Null(AssistantVoiceService.BaseType(null));
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(expected == response.StatusCode, $"{response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }
    private static async Task Code(HttpResponseMessage response, HttpStatusCode expected, string code) =>
        Assert.Equal(code, (await Json(response, expected)).GetProperty("code").GetString());
}
