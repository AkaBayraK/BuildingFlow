namespace BuildingFlow.Domain;

// Anketler: a management-area survey. Content (questions/options) is editable only while DRAFT;
// after publishing only the end date can move and the survey can be closed. Responses are immutable.
public enum SurveyCategory { DUES, SERVICE, SECURITY, SOCIAL, PARKING, ENVIRONMENT, GENERAL }
public enum SurveyStatus { DRAFT, PUBLISHED, CLOSED }
public enum SurveyAudience { ALL_RESIDENTS, OWNERS, TENANTS }
// PER_UNIT: one answer per independent unit (household); PER_ACCOUNT: one answer per resident account.
public enum SurveyResponseMode { PER_UNIT, PER_ACCOUNT }
public enum SurveyQuestionType { SINGLE_CHOICE, MULTIPLE_CHOICE, RATING, TEXT }

// Rows that must not reveal who wrote them: the audit interceptor stores a neutral actor and a day-truncated time.
public interface IAnonymousAudit { bool HideActor { get; } }

public sealed class Survey : BaseEntity
{
    public const int TitleLength = 200, DescriptionLength = 2000, MaxQuestions = 30;
    private readonly List<SurveyQuestion> questions = [];
    private Survey() { }
    public Survey(Guid propertyId, string title, string? description, SurveyCategory category, DateOnly startDate, DateOnly endDate, SurveyAudience audience, SurveyResponseMode responseMode, bool isAnonymous)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property required.");
        PropertyId = propertyId; Status = SurveyStatus.DRAFT;
        Edit(title, description, category, startDate, endDate, audience, responseMode, isAnonymous);
    }
    public Guid PropertyId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public SurveyCategory Category { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public SurveyAudience Audience { get; private set; }
    public SurveyResponseMode ResponseMode { get; private set; }
    public bool IsAnonymous { get; private set; }
    public SurveyStatus Status { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public int ResponseCount { get; private set; }
    // Eligible respondents frozen when the survey closes; null while it can still change.
    public int? EligibleCount { get; private set; }
    public IReadOnlyList<SurveyQuestion> Questions => questions;

    public void Edit(string title, string? description, SurveyCategory category, DateOnly startDate, DateOnly endDate, SurveyAudience audience, SurveyResponseMode responseMode, bool isAnonymous)
    {
        Draft();
        if (!Enum.IsDefined(category) || !Enum.IsDefined(audience) || !Enum.IsDefined(responseMode)) throw new ArgumentException("Invalid survey option.");
        Dates(startDate, endDate);
        Title = Required(title, nameof(title), TitleLength);
        Description = string.IsNullOrWhiteSpace(description) ? null : Required(description, nameof(description), DescriptionLength);
        Category = category; StartDate = startDate; EndDate = endDate; Audience = audience; ResponseMode = responseMode; IsAnonymous = isAnonymous;
    }
    public SurveyQuestion AddQuestion(string text, SurveyQuestionType type, bool isRequired, IReadOnlyList<string> options)
    {
        Draft();
        if (questions.Count >= MaxQuestions) throw new ArgumentException("Too many questions.");
        var question = new SurveyQuestion(this, questions.Count + 1, text, type, isRequired, options);
        questions.Add(question); return question;
    }
    // Draft edits replace the whole question list; the caller soft-deletes the returned rows and their options.
    public IReadOnlyList<SurveyQuestion> ClearQuestions()
    {
        Draft(); var removed = questions.ToArray(); questions.Clear(); return removed;
    }
    public void Publish(DateOnly today, DateTimeOffset at)
    {
        Draft();
        if (questions.Count == 0) throw new InvalidOperationException("A survey needs at least one question.");
        if (EndDate < today) throw new InvalidOperationException("The survey already ended.");
        Status = SurveyStatus.PUBLISHED; PublishedAt = at;
    }
    public void Extend(DateOnly endDate, DateOnly today)
    {
        if (Status != SurveyStatus.PUBLISHED) throw new InvalidOperationException("Only a published survey can be rescheduled.");
        Dates(StartDate, endDate);
        if (endDate < today) throw new InvalidOperationException("The end date is in the past.");
        EndDate = endDate;
    }
    public void Close(DateTimeOffset at, int eligible)
    {
        if (Status != SurveyStatus.PUBLISHED) throw new InvalidOperationException("Only a published survey can be closed.");
        Status = SurveyStatus.CLOSED; ClosedAt = at; EligibleCount = Math.Max(eligible, ResponseCount);
    }
    public bool IsOpen(DateOnly today) => Status == SurveyStatus.PUBLISHED && StartDate <= today && EndDate >= today;
    public void CountResponse() { if (Status != SurveyStatus.PUBLISHED) throw new InvalidOperationException("Survey is not open."); ResponseCount++; }
    private void Draft() { if (Status != SurveyStatus.DRAFT) throw new InvalidOperationException("Only a draft survey can be edited."); }
    private static void Dates(DateOnly start, DateOnly end)
    {
        if (start.Year is < 2000 or > 2100 || end.Year is < 2000 or > 2100) throw new ArgumentOutOfRangeException(nameof(start));
        if (end < start) throw new ArgumentException("End date precedes start date.");
    }
}

public sealed class SurveyQuestion : BaseEntity
{
    public const int TextLength = 500, MinOptions = 2, MaxOptions = 10;
    private readonly List<SurveyOption> options = [];
    private SurveyQuestion() { }
    internal SurveyQuestion(Survey survey, int position, string text, SurveyQuestionType type, bool isRequired, IReadOnlyList<string> values)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentException("Invalid question type.");
        var choice = type is SurveyQuestionType.SINGLE_CHOICE or SurveyQuestionType.MULTIPLE_CHOICE;
        if (choice ? values.Count is < MinOptions or > MaxOptions : values.Count != 0) throw new ArgumentException("Invalid option count.");
        if (values.Select(v => v.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase).Count() != values.Count) throw new ArgumentException("Duplicate option.");
        SurveyId = survey.Id; PropertyId = survey.PropertyId; Position = position; Text = Required(text, nameof(text), TextLength); Type = type; IsRequired = isRequired;
        for (var i = 0; i < values.Count; i++) options.Add(new SurveyOption(this, i + 1, values[i]));
    }
    public Guid SurveyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public int Position { get; private set; }
    public string Text { get; private set; } = null!;
    public SurveyQuestionType Type { get; private set; }
    public bool IsRequired { get; private set; }
    public IReadOnlyList<SurveyOption> Options => options;
}

public sealed class SurveyOption : BaseEntity
{
    public const int TextLength = 200;
    private SurveyOption() { }
    internal SurveyOption(SurveyQuestion question, int position, string text)
    { QuestionId = question.Id; SurveyId = question.SurveyId; PropertyId = question.PropertyId; Position = position; Text = Required(text, nameof(text), TextLength); }
    public Guid QuestionId { get; private set; }
    public Guid SurveyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public int Position { get; private set; }
    public string Text { get; private set; } = null!;
}

// Who has answered: guards one answer per ballot (unit or account). It never links to the answers of an anonymous survey.
public sealed class SurveyParticipation : BaseEntity
{
    public const int KeyLength = 100;
    private SurveyParticipation() { }
    public SurveyParticipation(Survey survey, string ballotKey, Guid unitId, string userId, OccupancyType role)
    {
        SurveyId = survey.Id; PropertyId = survey.PropertyId; BallotKey = Required(ballotKey, nameof(ballotKey), KeyLength);
        UnitId = unitId; RespondentUserId = Required(userId, nameof(userId), 200); Role = role;
    }
    public Guid SurveyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string BallotKey { get; private set; } = null!;
    public Guid UnitId { get; private set; }
    public string RespondentUserId { get; private set; } = null!;
    public OccupancyType Role { get; private set; }
}

public sealed class SurveyResponse : BaseEntity, IAnonymousAudit
{
    private SurveyResponse() { }
    public SurveyResponse(Survey survey, SurveyParticipation participation, DateOnly submittedOn)
    {
        SurveyId = survey.Id; PropertyId = survey.PropertyId; SubmittedOn = submittedOn; IsAnonymous = survey.IsAnonymous;
        if (!survey.IsAnonymous) { ParticipationId = participation.Id; UnitId = participation.UnitId; }
    }
    public Guid SurveyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid? ParticipationId { get; private set; }
    public Guid? UnitId { get; private set; }
    public DateOnly SubmittedOn { get; private set; }
    public bool IsAnonymous { get; private set; }
    public bool HideActor => IsAnonymous;
}

public sealed class SurveyAnswer : BaseEntity, IAnonymousAudit
{
    public const int TextLength = 1000;
    private SurveyAnswer() { }
    public SurveyAnswer(SurveyResponse response, SurveyQuestion question, Guid? optionId, int? rating, string? text)
    {
        if (rating is not null and (< 1 or > 5)) throw new ArgumentOutOfRangeException(nameof(rating));
        ResponseId = response.Id; SurveyId = response.SurveyId; PropertyId = response.PropertyId; QuestionId = question.Id; IsAnonymous = response.IsAnonymous;
        OptionId = optionId; Rating = rating; Text = text is null ? null : Required(text, nameof(text), TextLength);
    }
    public Guid ResponseId { get; private set; }
    public Guid SurveyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid? OptionId { get; private set; }
    public int? Rating { get; private set; }
    public string? Text { get; private set; }
    public bool IsAnonymous { get; private set; }
    public bool HideActor => IsAnonymous;
}
