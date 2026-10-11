using System.Text.Json.Nodes;
using BuildingFlow.Application;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class MessagingContractTests
{
    private static EmailRequested Email() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero), "tr", "recipient@example.test", "Duyuru", "Düz metin\nİkinci satır");
    [Fact] public void AllThreeVersionedContractsRoundtripWithoutLosingIdentity()
    {
        var e = Email();
        IMessageCommand[] commands = [e, new SmsRequested(e.MessageId,e.PropertyId,e.CorrelationId,e.OccurredAt,"en","+905551234567","Body"),
            new NotificationRequested(e.MessageId,e.PropertyId,e.CorrelationId,e.OccurredAt,"tr","subject-id","Title","Body")];
        foreach (var command in commands) Assert.Equal(command, MessageContracts.Decode(MessageContracts.Encode(command)));
    }
    [Fact] public void RetryEncodingRetainsMessageAndCorrelationIds()
    { var e=Email(); Assert.Equal(MessageContracts.Encode(e), MessageContracts.Encode(e)); }
    [Theory][InlineData("messageId")][InlineData("propertyId")][InlineData("correlationId")]
    public void EmptyIdentityCannotEnterTransport(string field)
    { var e=Email();e=field switch{"messageId"=>e with{MessageId=Guid.Empty},"propertyId"=>e with{PropertyId=Guid.Empty},_=>e with{CorrelationId=Guid.Empty}};Assert.Throws<InvalidMessageException>(()=>MessageContracts.Encode(e)); }
    [Theory][InlineData("recipient@example.test\r\nBcc: other@example.test")][InlineData("Name <recipient@example.test>")][InlineData("broken")][InlineData(" recipient@example.test")]
    public void EmailRejectsHeaderInjectionDisplayNameAndMalformedAddress(string recipient)
    { Assert.Throws<InvalidMessageException>(()=>MessageContracts.Encode(Email() with{Recipient=recipient})); }
    [Fact] public void NewlinesAreBodyOnlyAndMetadataMustBeUtcSupportedLocale()
    {
        var e=Email();Assert.Throws<InvalidMessageException>(()=>MessageContracts.Encode(e with{Subject="Title\nHeader"}));
        Assert.Throws<InvalidMessageException>(()=>MessageContracts.Encode(e with{Locale="de"}));
        Assert.Throws<InvalidMessageException>(()=>MessageContracts.Encode(e with{OccurredAt=e.OccurredAt.ToOffset(TimeSpan.FromHours(3))}));
        Assert.Throws<InvalidMessageException>(()=>MessageContracts.Encode(e with{OccurredAt=default}));
        Assert.Throws<InvalidMessageException>(()=>MessageContracts.Encode(e with{Body=new string('a',10001)}));
    }
    [Theory][InlineData("5551234567")][InlineData("+0123456789")][InlineData("+90555\n1234567")]
    public void SmsNeedsE164Recipient(string recipient)
    {var e=Email();Assert.Throws<InvalidMessageException>(()=>MessageContracts.Encode(new SmsRequested(e.MessageId,e.PropertyId,e.CorrelationId,e.OccurredAt,"tr",recipient,"Body")));}
    [Fact] public void NotificationHasExplicitSubjectAndTextLimits()
    {var e=Email();var n=new NotificationRequested(e.MessageId,e.PropertyId,e.CorrelationId,e.OccurredAt,"tr","actor","Title","Body");
        Assert.Throws<InvalidMessageException>(()=>MessageContracts.Encode(n with{RecipientSubject=" "}));
        Assert.Throws<InvalidMessageException>(()=>MessageContracts.Encode(n with{Body=new string('x',2001)}));}
    [Theory][InlineData("schemaVersion")][InlineData("type")][InlineData("messageId")][InlineData("propertyId")][InlineData("correlationId")]
    public void UnknownSchemaTypeOrMismatchedEnvelopeIsRejected(string field)
    {
        var json=JsonNode.Parse(MessageContracts.Encode(Email()))!;
        json[field]=field switch{"schemaVersion"=>JsonValue.Create(2),"type"=>JsonValue.Create("clr.type.other"),_=>JsonValue.Create(Guid.NewGuid().ToString())};
        Assert.Throws<InvalidMessageException>(()=>MessageContracts.Decode(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString())));
    }
    [Fact] public void ExtraFieldsOversizedAndMalformedJsonNeverLeakSensitiveContent()
    {
        var json=JsonNode.Parse(MessageContracts.Encode(Email()))!;json["payload"]!["$type"]="arbitrary";
        var error=Assert.Throws<InvalidMessageException>(()=>MessageContracts.Decode(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString())));
        Assert.DoesNotContain("recipient",error.Message);Assert.DoesNotContain("arbitrary",error.Message);
        Assert.Throws<InvalidMessageException>(()=>MessageContracts.Decode(new byte[MessageContracts.MaxBytes+1]));
        Assert.Throws<InvalidMessageException>(()=>MessageContracts.Decode("not-json"u8));
        Assert.Throws<InvalidMessageException>(()=>MessageContracts.Decode("null"u8));
    }
}
