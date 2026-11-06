using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class AutomationTests
{
    private static AutomationSettings Settings(int[] offsets, int send = 10, int quietStart = 21, int quietEnd = 9, bool inApp = true) =>
        new("Kural", null, null, send, quietStart, quietEnd, offsets, inApp, false, false, "Başlık", "Gövde");

    [Theory]
    [InlineData(21, 9, 22, true)] [InlineData(21, 9, 3, true)] [InlineData(21, 9, 9, false)] [InlineData(21, 9, 20, false)]
    [InlineData(1, 5, 2, true)] [InlineData(1, 5, 5, false)] [InlineData(0, 0, 12, false)]
    public void QuietWindowHandlesMidnight(int start, int end, int hour, bool quiet) => Assert.Equal(quiet, AutomationDefinition.InQuietWindow(start, end, hour));

    [Fact]
    public void NewRulesStartPausedAndEveryStateChangeBumpsRuleVersion()
    {
        var rule = new AutomationDefinition(Guid.NewGuid(), AutomationType.DUES_DUE_REMINDER, Settings([0, -3, -1]));
        Assert.Equal(AutomationStatus.PAUSED, rule.Status); Assert.Equal([-3, -1, 0], rule.OffsetDays); Assert.Equal(1, rule.RuleVersion);
        rule.Activate(DateTimeOffset.UtcNow); Assert.Equal(2, rule.RuleVersion); Assert.Throws<InvalidOperationException>(() => rule.Activate(DateTimeOffset.UtcNow));
        rule.Update(Settings([-2])); rule.Pause(); Assert.Equal(4, rule.RuleVersion);
    }

    [Fact]
    public void OffsetsMustMatchTheRuleType()
    {
        Assert.Throws<ArgumentException>(() => new AutomationDefinition(Guid.NewGuid(), AutomationType.DUES_DUE_REMINDER, Settings([1])));
        Assert.Throws<ArgumentException>(() => new AutomationDefinition(Guid.NewGuid(), AutomationType.DUES_OVERDUE_REMINDER, Settings([0])));
        Assert.Throws<ArgumentException>(() => new AutomationDefinition(Guid.NewGuid(), AutomationType.PAYMENT_RECEIVED, Settings([1])));
        Assert.Throws<ArgumentException>(() => new AutomationDefinition(Guid.NewGuid(), AutomationType.DUES_DUE_REMINDER, Settings([-1, -1])));
        Assert.Throws<ArgumentException>(() => new AutomationDefinition(Guid.NewGuid(), AutomationType.DUES_DUE_REMINDER, Settings([-1], send: 22)));
        Assert.Throws<ArgumentException>(() => new AutomationDefinition(Guid.NewGuid(), AutomationType.DUES_DUE_REMINDER, Settings([-1], inApp: false)));
        Assert.NotNull(new AutomationDefinition(Guid.NewGuid(), AutomationType.PAYMENT_RECEIVED, Settings([], send: 23)));
    }

    [Fact]
    public void ExecutionRetriesWithBackoffThenDeadLetters()
    {
        var rule = new AutomationDefinition(Guid.NewGuid(), AutomationType.DUES_DUE_REMINDER, Settings([-1]));
        var run = new AutomationExecution(rule, new DateOnly(2026, 1, 1), AutomationTrigger.SCHEDULE, Guid.NewGuid(), DateTimeOffset.UtcNow);
        for (var i = 1; i < AutomationExecution.MaxAttempts; i++) { run.Fail("DATABASE_ERROR", DateTimeOffset.UtcNow, TimeSpan.FromMinutes(i)); Assert.Equal(AutomationExecutionStatus.FAILED, run.Status); Assert.NotNull(run.NextAttemptAt); }
        run.Fail("DATABASE_ERROR", DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        Assert.Equal(AutomationExecutionStatus.DEAD_LETTER, run.Status); Assert.Null(run.NextAttemptAt);
    }

    [Fact]
    public void DefaultTemplatesUseOnlyDeclaredVariablesAndNoPersonalNames()
    {
        foreach (var info in AutomationCatalog.All)
        {
            var type = Enum.Parse<AutomationType>(info.Type);
            Assert.True(AutomationCatalog.TemplateFits(type, info.DefaultTitle)); Assert.True(AutomationCatalog.TemplateFits(type, info.DefaultBody));
            Assert.DoesNotContain("Name}}", info.DefaultBody.Replace("propertyName", ""));
            var sms = SmsTypes.Find(info.SmsTypeCode)!;
            Assert.Equal(SmsCategory.DUES_NOTIFICATION, sms.Category);
            Assert.All(SmsText.Placeholders(sms.DefaultContent), p => Assert.Contains(p, info.Variables));
            Assert.InRange(SmsText.Segments(SmsText.Render(sms.DefaultContent, info.Variables.ToDictionary(v => v, _ => "XXXXXXXXXXXX"))), 1, 3);
        }
    }

    [Fact]
    public void DispatchRecordsOnlyMaskedRecipientsAndCancelsOnlyQueued()
    {
        var rule = new AutomationDefinition(Guid.NewGuid(), AutomationType.DUES_DUE_REMINDER, Settings([-1]));
        var run = new AutomationExecution(rule, new DateOnly(2026, 1, 1), AutomationTrigger.SCHEDULE, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var row = new NotificationDispatch(run, rule.Type, new string('a', 64), NotificationChannel.SMS, Guid.NewGuid());
        row.QueuedSms(Guid.NewGuid(), SmsText.MaskPhone("+905551234567"));
        Assert.DoesNotContain("1234567", row.RecipientMasked);
        row.Cancel("CHARGE_PAID"); Assert.Equal(NotificationDispatchStatus.CANCELLED, row.Status);
        Assert.Throws<InvalidOperationException>(() => row.Cancel("CHARGE_PAID"));
    }
}
