using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
// Anketler: draft-only editing, question/option bounds, publish/extend/close transitions and anonymous response shape.
public sealed class SurveyTests
{
    private static readonly DateOnly Today = new(2026, 11, 1);
    private static Survey Draft(bool anonymous = false) => new(Guid.NewGuid(), " Aidat Artışı ", "  ", SurveyCategory.DUES, Today, Today.AddDays(14), SurveyAudience.ALL_RESIDENTS, SurveyResponseMode.PER_UNIT, anonymous);
    [Fact] public void DraftTrimsAndOrdersQuestionsAndOptions()
    {
        var s = Draft(); Assert.Equal("Aidat Artışı", s.Title); Assert.Null(s.Description); Assert.Equal(SurveyStatus.DRAFT, s.Status);
        var q = s.AddQuestion(" Artışı onaylıyor musunuz? ", SurveyQuestionType.SINGLE_CHOICE, true, [" Evet ", "Hayır"]);
        s.AddQuestion("Memnuniyet", SurveyQuestionType.RATING, false, []);
        Assert.Equal(new[] { 1, 2 }, s.Questions.Select(x => x.Position)); Assert.Equal("Artışı onaylıyor musunuz?", q.Text);
        Assert.Equal(new[] { "Evet", "Hayır" }, q.Options.Select(o => o.Text)); Assert.All(q.Options, o => Assert.Equal(s.PropertyId, o.PropertyId));
        var removed = s.ClearQuestions(); Assert.Equal(2, removed.Count); Assert.Empty(s.Questions);
    }
    [Fact] public void InvalidSurveysAndQuestionsAreRejected()
    {
        var p = Guid.NewGuid();
        Assert.ThrowsAny<ArgumentException>(() => new Survey(Guid.Empty, "T", null, SurveyCategory.GENERAL, Today, Today, SurveyAudience.OWNERS, SurveyResponseMode.PER_UNIT, false));
        Assert.ThrowsAny<ArgumentException>(() => new Survey(p, " ", null, SurveyCategory.GENERAL, Today, Today, SurveyAudience.OWNERS, SurveyResponseMode.PER_UNIT, false));
        Assert.ThrowsAny<ArgumentException>(() => new Survey(p, "T", null, SurveyCategory.GENERAL, Today, Today.AddDays(-1), SurveyAudience.OWNERS, SurveyResponseMode.PER_UNIT, false));
        Assert.ThrowsAny<ArgumentException>(() => new Survey(p, "T", null, (SurveyCategory)99, Today, Today, SurveyAudience.OWNERS, SurveyResponseMode.PER_UNIT, false));
        Assert.ThrowsAny<ArgumentException>(() => new Survey(p, "T", null, SurveyCategory.GENERAL, Today, new(2101, 1, 1), SurveyAudience.OWNERS, SurveyResponseMode.PER_UNIT, false));
        var s = Draft();
        Assert.ThrowsAny<ArgumentException>(() => s.AddQuestion("Q", SurveyQuestionType.SINGLE_CHOICE, true, ["Tek"]));
        Assert.ThrowsAny<ArgumentException>(() => s.AddQuestion("Q", SurveyQuestionType.MULTIPLE_CHOICE, true, ["A", " a "]));
        Assert.ThrowsAny<ArgumentException>(() => s.AddQuestion("Q", SurveyQuestionType.TEXT, true, ["A", "B"]));
        Assert.ThrowsAny<ArgumentException>(() => s.AddQuestion("Q", SurveyQuestionType.MULTIPLE_CHOICE, true, Enumerable.Range(1, 11).Select(i => $"O{i}").ToArray()));
        for (var i = 0; i < Survey.MaxQuestions; i++) s.AddQuestion($"Q{i}", SurveyQuestionType.TEXT, false, []);
        Assert.ThrowsAny<ArgumentException>(() => s.AddQuestion("Fazla", SurveyQuestionType.TEXT, false, []));
    }
    [Fact] public void PublishedSurveyIsLockedCanBeExtendedAndClosedOnce()
    {
        var s = Draft(); Assert.Throws<InvalidOperationException>(() => s.Publish(Today, DateTimeOffset.UtcNow));
        s.AddQuestion("Q", SurveyQuestionType.RATING, true, []);
        var ended = Draft(); ended.AddQuestion("Q", SurveyQuestionType.TEXT, true, []); Assert.Throws<InvalidOperationException>(() => ended.Publish(Today.AddDays(15), DateTimeOffset.UtcNow));
        s.Publish(Today, DateTimeOffset.UtcNow); Assert.True(s.IsOpen(Today)); Assert.False(s.IsOpen(Today.AddDays(15)));
        Assert.Throws<InvalidOperationException>(() => s.Edit("X", null, SurveyCategory.GENERAL, Today, Today, SurveyAudience.OWNERS, SurveyResponseMode.PER_UNIT, false));
        Assert.Throws<InvalidOperationException>(() => s.AddQuestion("X", SurveyQuestionType.TEXT, false, []));
        Assert.ThrowsAny<ArgumentException>(() => s.Extend(Today.AddDays(-1), Today.AddDays(1))); Assert.Throws<InvalidOperationException>(() => s.Extend(Today.AddDays(2), Today.AddDays(3)));
        s.Extend(Today.AddDays(30), Today); Assert.Equal(Today.AddDays(30), s.EndDate);
        s.CountResponse(); s.CountResponse(); s.Close(DateTimeOffset.UtcNow, 1);
        Assert.Equal(SurveyStatus.CLOSED, s.Status); Assert.Equal(2, s.EligibleCount);
        Assert.Throws<InvalidOperationException>(() => s.Close(DateTimeOffset.UtcNow, 5)); Assert.Throws<InvalidOperationException>(() => s.CountResponse());
    }
    [Fact] public void AnonymousResponsesKeepNoParticipantLinkAndHideTheActor()
    {
        var s = Draft(true); var q = s.AddQuestion("Görüş", SurveyQuestionType.TEXT, true, []); s.Publish(Today, DateTimeOffset.UtcNow);
        var unit = Guid.NewGuid(); var participation = new SurveyParticipation(s, "UNIT:" + unit, unit, "user-1", OccupancyType.OWNER);
        var response = new SurveyResponse(s, participation, Today); var answer = new SurveyAnswer(response, q, null, null, " Öneri ");
        Assert.Null(response.ParticipationId); Assert.Null(response.UnitId); Assert.True(response.HideActor); Assert.True(answer.HideActor); Assert.Equal("Öneri", answer.Text);
        var open = Draft(); var oq = open.AddQuestion("Puan", SurveyQuestionType.RATING, true, []); open.Publish(Today, DateTimeOffset.UtcNow);
        var named = new SurveyResponse(open, new SurveyParticipation(open, "UNIT:" + unit, unit, "user-1", OccupancyType.OWNER), Today);
        Assert.Equal(unit, named.UnitId); Assert.False(named.HideActor);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SurveyAnswer(named, oq, null, 6, null));
    }
}
