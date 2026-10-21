using BuildingFlow.Domain;
namespace BuildingFlow.Application;

// Anketler: managers build and analyse surveys; residents answer once through their own unit context.
public sealed record SurveyQuestionRequest(string? Text, string? Type, bool IsRequired, IReadOnlyList<string>? Options);
public sealed record SurveyRequest(string? Title, string? Description, string? Category, DateOnly? StartDate, DateOnly? EndDate, string? Audience, string? ResponseMode, bool IsAnonymous, IReadOnlyList<SurveyQuestionRequest>? Questions, long Version = 0);
public sealed record SurveyVersionRequest(long Version);
public sealed record SurveyScheduleRequest(DateOnly? EndDate, long Version);
public sealed record SurveyQuestionCommand(string Text, SurveyQuestionType Type, bool IsRequired, IReadOnlyList<string> Options);
public sealed record SurveyCommand(string Title, string? Description, SurveyCategory Category, DateOnly StartDate, DateOnly EndDate, SurveyAudience Audience, SurveyResponseMode ResponseMode, bool IsAnonymous, IReadOnlyList<SurveyQuestionCommand> Questions, long Version);

public sealed record SurveyOptionDto(Guid Id, int Position, string Text);
public sealed record SurveyQuestionDto(Guid Id, int Position, string Text, string Type, bool IsRequired, IReadOnlyList<SurveyOptionDto> Options);
// State is the status seen on the given business day: DRAFT, SCHEDULED, ACTIVE, EXPIRED (published, end passed) or COMPLETED (closed).
public sealed record SurveySummary(Guid Id, Guid PropertyId, string Title, string? Description, string Category, DateOnly StartDate, DateOnly EndDate, string Audience, string ResponseMode, bool IsAnonymous, string Status, string State, int ResponseCount, int EligibleCount, int QuestionCount, DateTimeOffset? PublishedAt, DateTimeOffset? ClosedAt, long Version, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);
public sealed record SurveyDetail(SurveySummary Survey, IReadOnlyList<SurveyQuestionDto> Questions);
public sealed record SurveyCount(string Key, int Count);
// StateCounts ignore the state filter and CategoryCounts ignore the category filter, so tabs and filters show what each choice returns.
public sealed record SurveyPage(DateOnly Today, IReadOnlyList<SurveySummary> Items, int Page, int PageSize, int TotalCount, IReadOnlyList<SurveyCount> StateCounts, IReadOnlyList<SurveyCount> CategoryCounts);
public sealed record SurveyQuery(int Page = 1, int PageSize = 10, string? Search = null, string? State = null, string? Category = null, DateOnly? From = null, DateOnly? To = null);
public sealed record SurveyOverview(DateOnly Today, int Total, int CreatedThisMonth, int Drafts, int Active, int Completed, int Expired, int Responses, int Eligible, int CompletedResponses, int CompletedEligible, IReadOnlyList<SurveyCount> Categories, IReadOnlyList<SurveySummary> TopParticipation, SurveySummary? LastCompleted);

public sealed record SurveyOptionResult(Guid Id, string Text, int Count);
public sealed record SurveyTextAnswer(string Text, string? UnitNumber, DateOnly SubmittedOn);
public sealed record SurveyQuestionResult(Guid Id, int Position, string Text, string Type, int Answered, IReadOnlyList<SurveyOptionResult> Options, decimal? RatingAverage, IReadOnlyList<int> RatingCounts, IReadOnlyList<SurveyTextAnswer> Texts);
// Hidden is true for an anonymous survey with fewer answers than MinAnonymousResults: no breakdown is returned.
public sealed record SurveyResults(SurveySummary Survey, bool Hidden, int MinAnonymousResults, IReadOnlyList<SurveyQuestionResult> Questions);

public sealed record ResidentSurveyContext(Guid PropertyId, Guid UnitId, string Role);
public sealed record ResidentSurveyItem(Guid Id, string Title, string? Description, string Category, DateOnly StartDate, DateOnly EndDate, bool IsAnonymous, string ResponseMode, int QuestionCount, bool Answered);
public sealed record ResidentSurveyDetail(ResidentSurveyItem Survey, IReadOnlyList<SurveyQuestionDto> Questions);
public sealed record SurveyAnswerRequest(Guid QuestionId, IReadOnlyList<Guid>? OptionIds, int? Rating, string? Text);
public sealed record ResidentSurveySubmit(Guid PropertyId, Guid UnitId, string? Role, IReadOnlyList<SurveyAnswerRequest>? Answers);
public sealed record SurveyAnswerCommand(Guid QuestionId, IReadOnlyList<Guid> OptionIds, int? Rating, string? Text);
public sealed record ResidentSurveyReceipt(Guid SurveyId, DateOnly SubmittedOn, bool IsAnonymous);

public interface ISurveyStore
{
    Task<SurveyPage?> List(Guid property, SurveyQuery query, DateOnly today, AccessScope access, CancellationToken token);
    Task<SurveyOverview?> Overview(Guid property, DateOnly today, AccessScope access, CancellationToken token);
    Task<SurveyDetail?> Get(Guid property, Guid id, DateOnly today, AccessScope access, CancellationToken token);
    Task<SurveyResults?> Results(Guid property, Guid id, DateOnly today, AccessScope access, CancellationToken token);
    Task<SurveyDetail?> Save(Guid property, Guid? id, SurveyCommand command, DateOnly today, AccessScope access, CancellationToken token);
    Task<SurveyDetail?> Copy(Guid property, Guid id, DateOnly today, AccessScope access, CancellationToken token);
    Task<SurveyDetail?> Publish(Guid property, Guid id, long version, DateOnly today, AccessScope access, CancellationToken token);
    Task<SurveyDetail?> Extend(Guid property, Guid id, DateOnly endDate, long version, DateOnly today, AccessScope access, CancellationToken token);
    Task<SurveyDetail?> Close(Guid property, Guid id, long version, DateOnly today, AccessScope access, CancellationToken token);
    Task<bool> Delete(Guid property, Guid id, long version, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<ResidentSurveyItem>> Open(string user, ResidentSurveyContext context, DateOnly today, CancellationToken token);
    Task<ResidentSurveyDetail> Detail(string user, ResidentSurveyContext context, Guid id, DateOnly today, CancellationToken token);
    Task<ResidentSurveyReceipt> Submit(string user, ResidentSurveyContext context, Guid id, IReadOnlyList<SurveyAnswerCommand> answers, DateOnly today, CancellationToken token);
}

public sealed class SurveyService(ISurveyStore store, ICurrentUserService user, TimeProvider clock)
{
    public const int MinAnonymousResults = 3, MaxTextAnswers = 100;
    public static readonly string[] States = ["DRAFT", "SCHEDULED", "ACTIVE", "EXPIRED", "COMPLETED"];
    private static readonly string[] ResidentRoles = ["OWNER", "TENANT", "RESIDENT"];
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    public DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zone).DateTime);
    private AccessScope Access()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" }.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    private string Resident() => user.IsAuthenticated && !string.IsNullOrWhiteSpace(user.UserId) ? user.UserId : throw new UnauthorizedAccessException();
    public static void Invalid(string field, ErrorDefinition? error = null) => throw new RequestValidationException(new() { [field] = [(error ?? ErrorCatalog.SurveyInvalid).Create()] });
    private static bool Name<T>(string? value) where T : struct, Enum => value is not null && Enum.GetNames<T>().Contains(value);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Version(long version) { if (version < 1) Invalid("version"); }

    public Task<SurveyPage?> List(Guid property, SurveyQuery query, CancellationToken token)
    {
        var access = Access();
        if (query.Page is < 1 or > 1000000 || query.PageSize is < 1 or > 100) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] });
        if (query.Search?.Length > 200) throw new RequestValidationException(new() { ["search"] = [ErrorCatalog.ErrorsSearchBounds.Create()] });
        if (!string.IsNullOrEmpty(query.State) && !States.Contains(query.State)) Invalid("state");
        if (!string.IsNullOrEmpty(query.Category) && !Name<SurveyCategory>(query.Category)) Invalid("category");
        if (query.From > query.To) Invalid("from");
        return store.List(property, query, Today(), access, token);
    }
    public Task<SurveyOverview?> Overview(Guid property, CancellationToken token) => store.Overview(property, Today(), Access(), token);
    public Task<SurveyDetail?> Get(Guid property, Guid id, CancellationToken token) => store.Get(property, id, Today(), Access(), token);
    public Task<SurveyResults?> Results(Guid property, Guid id, CancellationToken token) => store.Results(property, id, Today(), Access(), token);
    public Task<SurveyDetail?> Save(Guid property, Guid? id, SurveyRequest request, CancellationToken token)
    {
        var access = Access();
        if (id.HasValue ? request.Version < 1 : request.Version != 0) Invalid("version");
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > Survey.TitleLength) Invalid("title");
        if (request.Description?.Trim().Length > Survey.DescriptionLength) Invalid("description");
        if (!Name<SurveyCategory>(request.Category)) Invalid("category");
        if (!Name<SurveyAudience>(request.Audience)) Invalid("audience");
        if (!Name<SurveyResponseMode>(request.ResponseMode)) Invalid("responseMode");
        if (request.StartDate is not { Year: >= 2000 and <= 2100 }) Invalid("startDate");
        if (request.EndDate is not { Year: >= 2000 and <= 2100 } || request.EndDate < request.StartDate) Invalid("endDate");
        var questions = Questions(request.Questions ?? []);
        var command = new SurveyCommand(request.Title!.Trim(), Clean(request.Description), Enum.Parse<SurveyCategory>(request.Category!), request.StartDate!.Value, request.EndDate!.Value,
            Enum.Parse<SurveyAudience>(request.Audience!), Enum.Parse<SurveyResponseMode>(request.ResponseMode!), request.IsAnonymous, questions, request.Version);
        return store.Save(property, id, command, Today(), access, token);
    }
    private static IReadOnlyList<SurveyQuestionCommand> Questions(IReadOnlyList<SurveyQuestionRequest> values)
    {
        if (values.Count > Survey.MaxQuestions) Invalid("questions", ErrorCatalog.SurveyQuestionInvalid);
        var result = new List<SurveyQuestionCommand>();
        for (var i = 0; i < values.Count; i++)
        {
            var q = values[i]; var field = $"questions[{i}]";
            if (q is null || string.IsNullOrWhiteSpace(q.Text) || q.Text.Trim().Length > SurveyQuestion.TextLength || !Name<SurveyQuestionType>(q.Type)) Invalid(field, ErrorCatalog.SurveyQuestionInvalid);
            var type = Enum.Parse<SurveyQuestionType>(q!.Type!);
            var options = (q.Options ?? []).Select(o => o?.Trim() ?? "").ToArray();
            var choice = type is SurveyQuestionType.SINGLE_CHOICE or SurveyQuestionType.MULTIPLE_CHOICE;
            if (choice && (options.Length is < SurveyQuestion.MinOptions or > SurveyQuestion.MaxOptions || options.Any(o => o.Length is 0 or > SurveyOption.TextLength)
                || options.Distinct(StringComparer.CurrentCultureIgnoreCase).Count() != options.Length)) Invalid(field, ErrorCatalog.SurveyQuestionInvalid);
            if (!choice && options.Length != 0) Invalid(field, ErrorCatalog.SurveyQuestionInvalid);
            result.Add(new(q.Text!.Trim(), type, q.IsRequired, options));
        }
        return result;
    }
    public Task<SurveyDetail?> Copy(Guid property, Guid id, CancellationToken token) => store.Copy(property, id, Today(), Access(), token);
    public Task<SurveyDetail?> Publish(Guid property, Guid id, long version, CancellationToken token) { var access = Access(); Version(version); return store.Publish(property, id, version, Today(), access, token); }
    public Task<SurveyDetail?> Close(Guid property, Guid id, long version, CancellationToken token) { var access = Access(); Version(version); return store.Close(property, id, version, Today(), access, token); }
    public Task<SurveyDetail?> Extend(Guid property, Guid id, SurveyScheduleRequest request, CancellationToken token)
    {
        var access = Access(); Version(request.Version);
        if (request.EndDate is not { Year: >= 2000 and <= 2100 }) Invalid("endDate");
        return store.Extend(property, id, request.EndDate!.Value, request.Version, Today(), access, token);
    }
    public Task<bool> Delete(Guid property, Guid id, long version, CancellationToken token) { var access = Access(); Version(version); return store.Delete(property, id, version, access, token); }

    private static ResidentSurveyContext Context(Guid property, Guid unit, string? role)
    {
        if (property == Guid.Empty || unit == Guid.Empty || role is null || !ResidentRoles.Contains(role)) throw new UnauthorizedAccessException();
        return new(property, unit, role);
    }
    public Task<IReadOnlyList<ResidentSurveyItem>> Open(Guid property, Guid unit, string? role, CancellationToken token) => store.Open(Resident(), Context(property, unit, role), Today(), token);
    public Task<ResidentSurveyDetail> Detail(Guid property, Guid unit, string? role, Guid id, CancellationToken token) => store.Detail(Resident(), Context(property, unit, role), id, Today(), token);
    public Task<ResidentSurveyReceipt> Submit(Guid id, ResidentSurveySubmit request, CancellationToken token)
    {
        var resident = Resident(); var context = Context(request.PropertyId, request.UnitId, request.Role);
        var answers = request.Answers ?? [];
        if (answers.Count > Survey.MaxQuestions || answers.Any(a => a is null || a.QuestionId == Guid.Empty) || answers.Select(a => a.QuestionId).Distinct().Count() != answers.Count) Invalid("answers", ErrorCatalog.SurveyAnswerInvalid);
        var commands = answers.Select(a => new SurveyAnswerCommand(a.QuestionId, a.OptionIds ?? [], a.Rating, Clean(a.Text))).ToArray();
        if (commands.Any(a => a.Text?.Length > SurveyAnswer.TextLength || a.OptionIds.Count > SurveyQuestion.MaxOptions || a.OptionIds.Distinct().Count() != a.OptionIds.Count)) Invalid("answers", ErrorCatalog.SurveyAnswerInvalid);
        return store.Submit(resident, context, id, commands, Today(), token);
    }
}
