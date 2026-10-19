using System.Text.Json;
using System.Text.Json.Nodes;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class ConsentTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static string Hash(char c) => new(c, 64);

    // The web and mobile parsers run the same file; a change in one parser must change the vectors for all.
    public static TheoryData<string> Vectors()
    {
        var data = new TheoryData<string>();
        foreach (var vector in VectorFile().AsArray()) data.Add(vector!["name"]!.GetValue<string>());
        return data;
    }

    private static JsonNode VectorFile()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "docs", "api", "consent-markup-vectors.json");
            if (File.Exists(path)) return JsonNode.Parse(File.ReadAllText(path))!;
        }
        throw new FileNotFoundException("consent-markup-vectors.json");
    }

    [Theory] [MemberData(nameof(Vectors))]
    public void MarkupMatchesSharedVectors(string name)
    {
        var vector = VectorFile().AsArray().Single(v => v!["name"]!.GetValue<string>() == name)!;
        var result = ConsentMarkup.Parse(vector["input"]!.GetValue<string>());
        Assert.Equal(vector["error"]?.GetValue<string>(), result.Error);
        var actual = JsonSerializer.SerializeToNode(result.Blocks, Web);
        Assert.True(JsonNode.DeepEquals(vector["blocks"], actual), actual!.ToJsonString());
    }

    [Fact]
    public void MarkupLimitsLengthAndLines()
    {
        Assert.Equal(ConsentMarkup.ErrorTooLong, ConsentMarkup.Parse(new string('a', ConsentMarkup.MaxLength + 1)).Error);
        Assert.Equal(ConsentMarkup.ErrorTooManyLines, ConsentMarkup.Parse(string.Join('\n', Enumerable.Repeat("a", ConsentMarkup.MaxLines + 1))).Error);
        Assert.Equal(ConsentMarkup.ErrorControl, ConsentMarkup.Parse("a\uD800b").Error);
        Assert.Null(ConsentMarkup.Parse("emoji 😀 kalır").Error);
        Assert.Equal(ConsentMarkup.ErrorEmpty, ConsentMarkup.Parse(null).Error);
    }

    [Fact]
    public void TypeCodeKindAndNamesAreValidated()
    {
        Assert.Throws<ArgumentException>(() => new ConsentType("kvkk", "Ad", "Name", ConsentKind.INFORMATION, 1));
        Assert.Throws<ArgumentException>(() => new ConsentType("1ABC", "Ad", "Name", ConsentKind.INFORMATION, 1));
        Assert.Throws<ArgumentException>(() => new ConsentType("CAMERA", "Ad", "Name", (ConsentKind)9, 1));
        Assert.Throws<ArgumentException>(() => new ConsentType("CAMERA", "", "Name", ConsentKind.INFORMATION, 1));
        Assert.Throws<ArgumentException>(() => new ConsentType("CAMERA", "Ad", "Name", ConsentKind.INFORMATION, -1));
        var type = new ConsentType("CAMERA_RECORDING", " Kamera ", "Camera", ConsentKind.EXPLICIT_CONSENT, 40);
        Assert.Equal("Kamera", type.NameTr); Assert.False(type.IsSystem); Assert.Null(type.Feature);
    }

    [Fact]
    public void ConsentRowBindsTextTypeAndRevokesOnce()
    {
        var type = new ConsentType("CAMERA", "Kamera", "Camera", ConsentKind.EXPLICIT_CONSENT, 1);
        var other = new ConsentType("OTHER", "Diğer", "Other", ConsentKind.EXPLICIT_CONSENT, 2);
        var text = new ConsentText(type, 1, "Başlık", "Metin", ConsentEvidence.Sha256("Metin"), true, Guid.NewGuid(), Hash('a'));
        Assert.Throws<ArgumentException>(() => new UserConsent("user", "Ada Kaya", other, text, DateTimeOffset.UtcNow, ConsentChannel.MOBILE, null, Guid.NewGuid(), Hash('b')));
        Assert.Throws<ArgumentException>(() => new ConsentText(type, 0, "Başlık", "Metin", Hash('c'), true, Guid.NewGuid(), Hash('a')));
        Assert.Throws<ArgumentException>(() => new ConsentText(type, 1, "Başlık", "Metin", "ABC", true, Guid.NewGuid(), Hash('a')));
        var approvedAt = new DateTimeOffset(2026, 10, 16, 15, 4, 5, TimeSpan.FromHours(3)).AddTicks(1234567);
        var consent = new UserConsent("user", "Ada Kaya", type, text, approvedAt, ConsentChannel.MOBILE, "1.4.0", Guid.NewGuid(), Hash('b'));
        Assert.Equal(TimeSpan.Zero, consent.ApprovedAt.Offset); Assert.Equal(0, consent.ApprovedAt.Ticks % 10);
        consent.Seal(ConsentEvidence.Compute(consent, type.Code));
        Assert.Throws<InvalidOperationException>(() => consent.Seal(Hash('d')));
        consent.Revoke(ConsentRevokeReason.WITHDRAWN, approvedAt.AddDays(1), Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => consent.Revoke(ConsentRevokeReason.SUPERSEDED, approvedAt.AddDays(2), null));
    }

    [Fact]
    public void EvidenceChangesWhenAnyBoundFieldChanges()
    {
        var type = new ConsentType("CAMERA", "Kamera", "Camera", ConsentKind.EXPLICIT_CONSENT, 1);
        var text = new ConsentText(type, 1, "Başlık", "Metin", ConsentEvidence.Sha256("Metin"), true, Guid.NewGuid(), Hash('a'));
        var operation = Guid.NewGuid(); var at = DateTimeOffset.UtcNow;
        var a = new UserConsent("user", "Ada Kaya", type, text, at, ConsentChannel.MOBILE, null, operation, Hash('b'));
        var evidence = ConsentEvidence.Compute(a, type.Code);
        Assert.Matches("^[0-9a-f]{64}$", evidence);
        Assert.Equal(evidence, ConsentEvidence.Compute(a, type.Code));
        Assert.NotEqual(evidence, ConsentEvidence.Compute(a, "OTHER"));
        var renamed = new UserConsent("user", "Ada Kaya ", type, text, at, ConsentChannel.MOBILE, null, operation, Hash('b'));
        Assert.Equal(ConsentEvidence.Compute(renamed, type.Code) == evidence, renamed.Id == a.Id);
        Assert.NotEqual(ConsentEvidence.TextFingerprint("T", "B", true), ConsentEvidence.TextFingerprint("T", "B", false));
    }
}
