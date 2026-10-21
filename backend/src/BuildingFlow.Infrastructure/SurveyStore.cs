using System.Data;
using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;
public sealed class SurveyStore(BuildingFlowDbContext db, IMobileSessionStore sessions, ICurrentUserService actor, TimeProvider clock) : ISurveyStore
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    private IQueryable<Property> Properties(AccessScope scope) => db.Properties.Where(p => !p.IsDeleted && (scope.ReadAll || db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.UserId == scope.UserId && m.IsEnabled && !m.IsDeleted)));
    private IQueryable<Survey> Surveys(Guid property) => db.Surveys.AsNoTracking().Where(x => x.PropertyId == property && x.IsEnabled);
    private static string Pattern(string search) => "%" + search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    private static void Version(long expected, long actual) { if (expected != actual) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create()); }
    private static HierarchyConflictException State() => new(ErrorCatalog.SurveyState.Create());
    private static OccupancyType[] Types(SurveyAudience audience) => audience switch
    {
        SurveyAudience.OWNERS => [OccupancyType.OWNER],
        SurveyAudience.TENANTS => [OccupancyType.TENANT, OccupancyType.RESIDENT],
        _ => [OccupancyType.OWNER, OccupancyType.TENANT, OccupancyType.RESIDENT],
    };
    public static string StateOf(Survey x, DateOnly today) => x.Status switch
    {
        SurveyStatus.DRAFT => "DRAFT",
        SurveyStatus.CLOSED => "COMPLETED",
        _ when x.StartDate > today => "SCHEDULED",
        _ when x.EndDate < today => "EXPIRED",
        _ => "ACTIVE",
    };
    private static IQueryable<Survey> InState(IQueryable<Survey> rows, string state, DateOnly today) => state switch
    {
        "DRAFT" => rows.Where(x => x.Status == SurveyStatus.DRAFT),
        "COMPLETED" => rows.Where(x => x.Status == SurveyStatus.CLOSED),
        "SCHEDULED" => rows.Where(x => x.Status == SurveyStatus.PUBLISHED && x.StartDate > today),
        "EXPIRED" => rows.Where(x => x.Status == SurveyStatus.PUBLISHED && x.EndDate < today),
        _ => rows.Where(x => x.Status == SurveyStatus.PUBLISHED && x.StartDate <= today && x.EndDate >= today),
    };

    // Live denominator: units (PER_UNIT) or linked resident accounts (PER_ACCOUNT) with an active matching relationship today.
    private async Task<int> Eligible(Guid property, SurveyAudience audience, SurveyResponseMode mode, DateOnly today, CancellationToken token)
    {
        var types = Types(audience);
        var links = db.UnitOccupancies.AsNoTracking().Where(o => o.PropertyId == property && o.IsEnabled && types.Contains(o.Type) && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)
            && db.Units.Any(u => u.Id == o.UnitId && u.IsEnabled));
        if (mode == SurveyResponseMode.PER_UNIT) return await links.Select(o => o.UnitId).Distinct().CountAsync(token);
        return await links.Join(db.People.Where(p => p.IsEnabled && p.LinkedUserId != null), o => o.PersonId, p => p.Id, (o, p) => p.LinkedUserId).Distinct().CountAsync(token);
    }
    private sealed class EligibleCache(SurveyStore store, Guid property, DateOnly today)
    {
        private readonly Dictionary<(SurveyAudience, SurveyResponseMode), int> values = [];
        public async Task<int> For(Survey x, CancellationToken token)
        {
            if (x.EligibleCount is { } frozen) return frozen;
            if (!values.TryGetValue((x.Audience, x.ResponseMode), out var count)) values[(x.Audience, x.ResponseMode)] = count = await store.Eligible(property, x.Audience, x.ResponseMode, today, token);
            return Math.Max(count, x.ResponseCount);
        }
    }
    private static SurveySummary Summary(Survey x, int eligible, int questions, DateOnly today) => new(x.Id, x.PropertyId, x.Title, x.Description, x.Category.ToString(), x.StartDate, x.EndDate, x.Audience.ToString(), x.ResponseMode.ToString(),
        x.IsAnonymous, x.Status.ToString(), StateOf(x, today), x.ResponseCount, eligible, questions, x.PublishedAt, x.ClosedAt, x.Version, x.CreatedAt, x.ModifiedAt);
    private static SurveyQuestionDto Question(SurveyQuestion q) => new(q.Id, q.Position, q.Text, q.Type.ToString(), q.IsRequired, q.Options.OrderBy(o => o.Position).Select(o => new SurveyOptionDto(o.Id, o.Position, o.Text)).ToArray());
    private async Task<SurveyDetail> Detail(Survey x, DateOnly today, CancellationToken token)
    {
        var eligible = await new EligibleCache(this, x.PropertyId, today).For(x, token);
        var questions = x.Questions.OrderBy(q => q.Position).Select(Question).ToArray();
        return new(Summary(x, eligible, questions.Length, today), questions);
    }
    private Task<Survey?> Load(Guid property, Guid id, bool track, CancellationToken token)
    {
        var rows = db.Surveys.Include(x => x.Questions).ThenInclude(q => q.Options).AsSplitQuery().Where(x => x.PropertyId == property && x.Id == id && x.IsEnabled);
        return (track ? rows : rows.AsNoTracking()).SingleOrDefaultAsync(token);
    }
    private async Task<Property?> Lock(Guid property, AccessScope scope, CancellationToken token)
    {
        if (!await Properties(scope).AnyAsync(p => p.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        var parent = await Properties(scope).SingleOrDefaultAsync(p => p.Id == property, token);
        if (parent is not null && !parent.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        return parent;
    }

    public async Task<SurveyPage?> List(Guid property, SurveyQuery query, DateOnly today, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(p => p.Id == property, token)) return null;
        var rows = Surveys(property);
        if (query.From.HasValue) rows = rows.Where(x => x.EndDate >= query.From.Value);
        if (query.To.HasValue) rows = rows.Where(x => x.StartDate <= query.To.Value);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var pattern = Pattern(query.Search); rows = rows.Where(x => EF.Functions.ILike(x.Title, pattern, "\\") || EF.Functions.ILike(x.Description ?? "", pattern, "\\")); }
        var byState = rows;
        if (!string.IsNullOrEmpty(query.Category)) { var category = Enum.Parse<SurveyCategory>(query.Category); byState = rows.Where(x => x.Category == category); }
        var states = new List<SurveyCount>();
        foreach (var state in SurveyService.States) states.Add(new(state, await InState(byState, state, today).CountAsync(token)));
        var byCategory = string.IsNullOrEmpty(query.State) ? rows : InState(rows, query.State, today);
        var grouped = await byCategory.GroupBy(x => x.Category).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(token);
        var categories = Enum.GetValues<SurveyCategory>().Select(c => new SurveyCount(c.ToString(), grouped.SingleOrDefault(g => g.Key == c)?.Count ?? 0)).ToArray();
        var filtered = string.IsNullOrEmpty(query.State) ? byState : InState(byState, query.State, today);
        var total = await filtered.CountAsync(token);
        var values = await filtered.OrderByDescending(x => x.StartDate).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        var ids = values.Select(v => v.Id).ToArray();
        var questionCounts = await db.SurveyQuestions.AsNoTracking().Where(q => ids.Contains(q.SurveyId)).GroupBy(q => q.SurveyId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, token);
        var eligible = new EligibleCache(this, property, today); var items = new List<SurveySummary>();
        foreach (var v in values) items.Add(Summary(v, await eligible.For(v, token), questionCounts.GetValueOrDefault(v.Id), today));
        await tx.CommitAsync(token);
        return new(today, items, query.Page, query.PageSize, total, states, categories);
    }

    public async Task<SurveyOverview?> Overview(Guid property, DateOnly today, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(p => p.Id == property, token)) return null;
        var all = await Surveys(property).ToListAsync(token);
        var questionCounts = await db.SurveyQuestions.AsNoTracking().Where(q => q.PropertyId == property).GroupBy(q => q.SurveyId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, token);
        var eligible = new EligibleCache(this, property, today); var items = new List<SurveySummary>();
        foreach (var v in all) items.Add(Summary(v, v.Status == SurveyStatus.DRAFT ? 0 : await eligible.For(v, token), questionCounts.GetValueOrDefault(v.Id), today));
        var monthStart = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, Zone.GetUtcOffset(new DateTime(today.Year, today.Month, 1)));
        var live = items.Where(x => x.Status != "DRAFT").ToArray(); var done = items.Where(x => x.State == "COMPLETED").ToArray();
        var top = live.Where(x => x.EligibleCount > 0 && x.ResponseCount > 0).OrderByDescending(x => (decimal)x.ResponseCount / x.EligibleCount).ThenByDescending(x => x.ResponseCount).ThenBy(x => x.Id).Take(3).ToArray();
        var categories = Enum.GetValues<SurveyCategory>().Select(c => new SurveyCount(c.ToString(), items.Count(x => x.Category == c.ToString()))).ToArray();
        await tx.CommitAsync(token);
        return new(today, items.Count, items.Count(x => x.CreatedAt >= monthStart), items.Count(x => x.State == "DRAFT"), items.Count(x => x.State is "ACTIVE" or "SCHEDULED"), done.Length, items.Count(x => x.State == "EXPIRED"),
            live.Sum(x => x.ResponseCount), live.Sum(x => x.EligibleCount), done.Sum(x => x.ResponseCount), done.Sum(x => x.EligibleCount), categories, top,
            done.OrderByDescending(x => x.ClosedAt).ThenBy(x => x.Id).FirstOrDefault());
    }

    public async Task<SurveyDetail?> Get(Guid property, Guid id, DateOnly today, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(p => p.Id == property, token)) return null;
        var value = await Load(property, id, false, token); return value is null ? null : await Detail(value, today, token);
    }

    public async Task<SurveyResults?> Results(Guid property, Guid id, DateOnly today, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(p => p.Id == property, token)) return null;
        var survey = await Load(property, id, false, token); if (survey is null) return null;
        var detail = await Detail(survey, today, token);
        if (survey.IsAnonymous && survey.ResponseCount < SurveyService.MinAnonymousResults) { await tx.CommitAsync(token); return new(detail.Survey, true, SurveyService.MinAnonymousResults, []); }
        var answers = db.SurveyAnswers.AsNoTracking().Where(a => a.PropertyId == property && a.SurveyId == id);
        var answered = await answers.GroupBy(a => a.QuestionId).Select(g => new { g.Key, Count = g.Select(a => a.ResponseId).Distinct().Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, token);
        var options = await answers.Where(a => a.OptionId != null).GroupBy(a => a.OptionId!.Value).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, token);
        var ratings = await answers.Where(a => a.Rating != null).GroupBy(a => new { a.QuestionId, Rating = a.Rating!.Value }).Select(g => new { g.Key.QuestionId, g.Key.Rating, Count = g.Count() }).ToListAsync(token);
        var texts = await answers.Where(a => a.Text != null)
            .Join(db.SurveyResponses.AsNoTracking(), a => a.ResponseId, r => r.Id, (a, r) => new { a.QuestionId, a.Text, r.SubmittedOn, r.UnitId, a.Id })
            .OrderByDescending(x => x.SubmittedOn).ThenBy(x => x.Id).ToListAsync(token);
        var unitIds = texts.Where(t => t.UnitId != null).Select(t => t.UnitId!.Value).Distinct().ToArray();
        var units = survey.IsAnonymous ? [] : await db.Units.IgnoreQueryFilters().AsNoTracking().Where(u => u.PropertyId == property && unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Number, token);
        var questions = survey.Questions.OrderBy(q => q.Position).Select(q =>
        {
            var counts = Enumerable.Range(1, 5).Select(r => ratings.SingleOrDefault(x => x.QuestionId == q.Id && x.Rating == r)?.Count ?? 0).ToArray();
            var rated = counts.Sum(); decimal? average = rated == 0 ? null : Math.Round(counts.Select((c, i) => c * (i + 1m)).Sum() / rated, 2);
            var free = texts.Where(t => t.QuestionId == q.Id).Take(SurveyService.MaxTextAnswers)
                .Select(t => new SurveyTextAnswer(t.Text!, survey.IsAnonymous || t.UnitId is null ? null : units.GetValueOrDefault(t.UnitId.Value), t.SubmittedOn)).ToArray();
            return new SurveyQuestionResult(q.Id, q.Position, q.Text, q.Type.ToString(), answered.GetValueOrDefault(q.Id),
                q.Options.OrderBy(o => o.Position).Select(o => new SurveyOptionResult(o.Id, o.Text, options.GetValueOrDefault(o.Id))).ToArray(),
                q.Type == SurveyQuestionType.RATING ? average : null, q.Type == SurveyQuestionType.RATING ? counts : [], free);
        }).ToArray();
        await tx.CommitAsync(token);
        return new(detail.Survey, false, SurveyService.MinAnonymousResults, questions);
    }

    public async Task<SurveyDetail?> Save(Guid property, Guid? id, SurveyCommand c, DateOnly today, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        Survey value;
        if (id.HasValue)
        {
            var current = await Load(property, id.Value, true, token); if (current is null) return null;
            Version(c.Version, current.Version); if (current.Status != SurveyStatus.DRAFT) throw State(); value = current;
            value.Edit(c.Title, c.Description, c.Category, c.StartDate, c.EndDate, c.Audience, c.ResponseMode, c.IsAnonymous);
            foreach (var old in value.ClearQuestions()) { foreach (var option in old.Options) db.Remove(option); db.Remove(old); }
            db.Entry(value).Property(x => x.IsEnabled).IsModified = true;
        }
        else { value = new(property, c.Title, c.Description, c.Category, c.StartDate, c.EndDate, c.Audience, c.ResponseMode, c.IsAnonymous); db.Add(value); }
        foreach (var q in c.Questions) db.Add(value.AddQuestion(q.Text, q.Type, q.IsRequired, q.Options));
        db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return await Detail(value, today, token);
    }

    public async Task<SurveyDetail?> Copy(Guid property, Guid id, DateOnly today, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var source = await Load(property, id, false, token); if (source is null) return null;
        // The copy keeps the same duration but never starts in the past.
        var start = source.StartDate < today ? today : source.StartDate; var end = start.AddDays(source.EndDate.DayNumber - source.StartDate.DayNumber);
        if (end.Year > 2100) { start = source.StartDate; end = source.EndDate; }
        var copy = new Survey(property, source.Title, source.Description, source.Category, start, end, source.Audience, source.ResponseMode, source.IsAnonymous); db.Add(copy);
        foreach (var q in source.Questions.OrderBy(q => q.Position)) db.Add(copy.AddQuestion(q.Text, q.Type, q.IsRequired, q.Options.OrderBy(o => o.Position).Select(o => o.Text).ToArray()));
        db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return await Detail(copy, today, token);
    }

    private async Task<SurveyDetail?> Change(Guid property, Guid id, long version, DateOnly today, AccessScope access, Func<Survey, Task> change, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var value = await Load(property, id, true, token); if (value is null) return null;
        Version(version, value.Version);
        try { await change(value); } catch (InvalidOperationException) { throw State(); } catch (ArgumentException) { throw State(); }
        db.Entry(value).Property(x => x.IsEnabled).IsModified = true; db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return await Detail(value, today, token);
    }
    public Task<SurveyDetail?> Publish(Guid property, Guid id, long version, DateOnly today, AccessScope access, CancellationToken token) =>
        Change(property, id, version, today, access, x => { x.Publish(today, clock.GetUtcNow()); return Task.CompletedTask; }, token);
    public Task<SurveyDetail?> Extend(Guid property, Guid id, DateOnly endDate, long version, DateOnly today, AccessScope access, CancellationToken token) =>
        Change(property, id, version, today, access, x => { x.Extend(endDate, today); return Task.CompletedTask; }, token);
    public Task<SurveyDetail?> Close(Guid property, Guid id, long version, DateOnly today, AccessScope access, CancellationToken token) =>
        Change(property, id, version, today, access, async x => {
            if (x.Status != SurveyStatus.PUBLISHED) throw new InvalidOperationException();
            x.Close(clock.GetUtcNow(), await Eligible(property, x.Audience, x.ResponseMode, today, token));
        }, token);

    public async Task<bool> Delete(Guid property, Guid id, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return false;
        var value = await Load(property, id, true, token); if (value is null) return false;
        Version(version, value.Version); if (value.Status != SurveyStatus.DRAFT) throw State();
        foreach (var old in value.ClearQuestions()) { foreach (var option in old.Options) db.Remove(option); db.Remove(old); }
        db.Remove(value); db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return true;
    }

    // Residents: the unit context must be one of the caller's real, active session contexts and match the survey audience.
    private async Task Authorize(string user, ResidentSurveyContext c, CancellationToken token)
    {
        var roles = new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT" }.Where(actor.IsInRole).ToArray();
        var session = await sessions.ReadAsync(user, actor.Username, roles, token);
        if (session.HasMore || !session.Contexts.Any(x => x.PropertyId == c.PropertyId && x.UnitId == c.UnitId && x.Role == c.Role)) throw new UnauthorizedAccessException();
        if (!await db.Properties.AsNoTracking().AnyAsync(p => p.Id == c.PropertyId && p.IsEnabled, token)) throw new UnauthorizedAccessException();
    }
    private static bool Audience(Survey x, string role) => Types(x.Audience).Contains(Enum.Parse<OccupancyType>(role));
    private static string Ballot(Survey x, string user, Guid unit) => x.ResponseMode == SurveyResponseMode.PER_UNIT ? $"UNIT:{unit}" : "ACCOUNT:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(user)));
    private IQueryable<Survey> Visible(ResidentSurveyContext c, DateOnly today) => db.Surveys.AsNoTracking().Where(x => x.PropertyId == c.PropertyId && x.IsEnabled && x.Status == SurveyStatus.PUBLISHED && x.StartDate <= today && x.EndDate >= today);
    private async Task<ResidentSurveyItem> Item(Survey x, string user, ResidentSurveyContext c, CancellationToken token)
    {
        var key = Ballot(x, user, c.UnitId);
        var answered = await db.SurveyParticipations.AsNoTracking().AnyAsync(p => p.SurveyId == x.Id && p.BallotKey == key, token);
        var questions = await db.SurveyQuestions.AsNoTracking().CountAsync(q => q.SurveyId == x.Id, token);
        return new(x.Id, x.Title, x.Description, x.Category.ToString(), x.StartDate, x.EndDate, x.IsAnonymous, x.ResponseMode.ToString(), questions, answered);
    }
    public async Task<IReadOnlyList<ResidentSurveyItem>> Open(string user, ResidentSurveyContext c, DateOnly today, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        await Authorize(user, c, token);
        var rows = (await Visible(c, today).OrderBy(x => x.EndDate).ThenBy(x => x.Id).Take(50).ToListAsync(token)).Where(x => Audience(x, c.Role)).ToArray();
        var items = new List<ResidentSurveyItem>(); foreach (var x in rows) items.Add(await Item(x, user, c, token));
        await tx.CommitAsync(token); return items;
    }
    public async Task<ResidentSurveyDetail> Detail(string user, ResidentSurveyContext c, Guid id, DateOnly today, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        await Authorize(user, c, token);
        var x = await Visible(c, today).Include(s => s.Questions).ThenInclude(q => q.Options).AsSplitQuery().SingleOrDefaultAsync(s => s.Id == id, token);
        if (x is null || !Audience(x, c.Role)) throw new UnauthorizedAccessException();
        var item = await Item(x, user, c, token); await tx.CommitAsync(token);
        return new(item, x.Questions.OrderBy(q => q.Position).Select(Question).ToArray());
    }
    public async Task<ResidentSurveyReceipt> Submit(string user, ResidentSurveyContext c, Guid id, IReadOnlyList<SurveyAnswerCommand> answers, DateOnly today, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await Authorize(user, c, token);
        if (!await db.Surveys.AnyAsync(x => x.PropertyId == c.PropertyId && x.Id == id && x.IsEnabled, token)) throw new UnauthorizedAccessException();
        // Serialises answers per survey: the counter, the open window and the ballot check see one consistent state.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM surveys WHERE id = {id} FOR UPDATE", token);
        var survey = await db.Surveys.Include(s => s.Questions).ThenInclude(q => q.Options).AsSplitQuery().SingleAsync(x => x.PropertyId == c.PropertyId && x.Id == id, token);
        if (!Audience(survey, c.Role)) throw new UnauthorizedAccessException();
        if (!survey.IsOpen(today)) throw new HierarchyConflictException(ErrorCatalog.SurveyNotOpen.Create());
        var key = Ballot(survey, user, c.UnitId);
        if (await db.SurveyParticipations.IgnoreQueryFilters().AnyAsync(p => p.SurveyId == id && p.BallotKey == key, token)) throw new HierarchyConflictException(ErrorCatalog.SurveyAlreadyAnswered.Create());
        var rows = Answers(survey, answers);
        var participation = new SurveyParticipation(survey, key, c.UnitId, user, Enum.Parse<OccupancyType>(c.Role));
        var response = new SurveyResponse(survey, participation, today);
        db.Add(participation); db.Add(response);
        foreach (var (question, option, rating, text) in rows) db.Add(new SurveyAnswer(response, question, option, rating, text));
        survey.CountResponse(); db.Entry(survey).Property(x => x.IsEnabled).IsModified = true;
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { throw new HierarchyConflictException(ErrorCatalog.SurveyAlreadyAnswered.Create()); }
        await tx.CommitAsync(token);
        return new(id, today, survey.IsAnonymous);
    }
    private static List<(SurveyQuestion, Guid?, int?, string?)> Answers(Survey survey, IReadOnlyList<SurveyAnswerCommand> answers)
    {
        var byId = survey.Questions.ToDictionary(q => q.Id); var rows = new List<(SurveyQuestion, Guid?, int?, string?)>();
        if (answers.Any(a => !byId.ContainsKey(a.QuestionId))) SurveyService.Invalid("answers", ErrorCatalog.SurveyAnswerInvalid);
        foreach (var q in survey.Questions)
        {
            var a = answers.SingleOrDefault(x => x.QuestionId == q.Id);
            var empty = a is null || (a.OptionIds.Count == 0 && a.Rating is null && a.Text is null);
            if (empty) { if (q.IsRequired) SurveyService.Invalid("answers", ErrorCatalog.SurveyAnswerInvalid); continue; }
            var options = q.Options.Select(o => o.Id).ToHashSet();
            var valid = q.Type switch
            {
                SurveyQuestionType.SINGLE_CHOICE => a!.OptionIds.Count == 1 && a.Rating is null && a.Text is null,
                SurveyQuestionType.MULTIPLE_CHOICE => a!.OptionIds.Count >= 1 && a.Rating is null && a.Text is null,
                SurveyQuestionType.RATING => a!.OptionIds.Count == 0 && a.Rating is >= 1 and <= 5 && a.Text is null,
                _ => a!.OptionIds.Count == 0 && a.Rating is null && a.Text is not null,
            };
            if (!valid || a!.OptionIds.Any(o => !options.Contains(o))) SurveyService.Invalid("answers", ErrorCatalog.SurveyAnswerInvalid);
            if (q.Type is SurveyQuestionType.SINGLE_CHOICE or SurveyQuestionType.MULTIPLE_CHOICE) rows.AddRange(a!.OptionIds.Select(o => (q, (Guid?)o, (int?)null, (string?)null)));
            else rows.Add((q, null, a!.Rating, a.Text));
        }
        if (rows.Count == 0) SurveyService.Invalid("answers", ErrorCatalog.SurveyAnswerInvalid);
        return rows;
    }
}
