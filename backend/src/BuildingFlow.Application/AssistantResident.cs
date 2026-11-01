using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
namespace BuildingFlow.Application;

// AI Asistan FAZ 3: resident assistant on mobile. Off by default; it also needs the caller's active AI_ASSISTANT consent on every
// request. Tools read only the caller's own unit context through the existing mobile stores, which reject any other context.
public static class ResidentAssistantTools
{
    public const string Balance = "RESIDENT_BALANCE", Requests = "RESIDENT_REQUESTS", Notices = "RESIDENT_NOTICES";
    public static readonly IReadOnlyList<string> All = [Balance, Requests, Notices];
    public static readonly string[] Roles = ["OWNER", "TENANT"];
    public static string Description(string tool) => tool switch
    {
        Balance => "Sakinin bu bölümdeki kendi borcu, geciken tutarı, bir sonraki son ödeme tarihi ve son ödemeleri",
        Requests => "Sakinin bu bölümden açtığı kendi taleplerinin durumları",
        Notices => "Sakine yayınlanmış son duyurular, okunmamış duyuru sayısı ve yayınlanmış belge tarihleri",
        _ => throw new ArgumentOutOfRangeException(nameof(tool))
    };
}

public sealed record ResidentAssistantSettings(bool Enabled, int DailyLimit)
{
    public const int DefaultLimit = 10;
}
public interface IAssistantConsent
{
    Task<bool> Active(string user, CancellationToken token);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ResidentChatRequest(Guid PropertyId, Guid UnitId, string? Role, string? PromptCode, string? Message, string? Locale);
public sealed record ResidentAssistantStatus(bool Enabled, string Mode, AssistantQuota Quota, IReadOnlyList<string> Tools, bool ReadOnly = true);

public sealed class ResidentAssistantService(IAssistantStore store, IAssistantModel model, ResidentAssistantSettings settings, IAssistantConsent consent,
    IMobileFinanceStore finance, IMobileSyncStore requests, IMobileNoticeStore notices, CommunicationService communication, ICurrentUserService user, TimeProvider clock)
{
    public const int Listed = 5, ExcerptLength = 400;
    public static readonly IReadOnlyCollection<string> Kinds = ["RESIDENT_PROMPT", "RESIDENT_FREE"];
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    private static readonly Regex CodePattern = new("^[A-Z][A-Z0-9_]{1,39}$", RegexOptions.CultureInvariant);
    private DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zone).DateTime);
    private static DateOnly Day(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);
    private AssistantQuota Quota(int used) => new(settings.DailyLimit, used, Math.Max(0, settings.DailyLimit - used));

    // Checked on every call, so withdrawing consent or switching the flag off closes the assistant immediately.
    private async Task<string> Gate(CancellationToken token)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId)) throw new UnauthorizedAccessException();
        if (!settings.Enabled) throw new CodedForbiddenException(ErrorCatalog.AssistantResidentDisabled.Create());
        if (!await consent.Active(user.UserId, token)) throw new CodedForbiddenException(ErrorCatalog.ConsentRequired.Create());
        return user.UserId;
    }

    public async Task<ResidentAssistantStatus> Status(CancellationToken token)
    {
        var actor = await Gate(token);
        return new(true, model.Enabled ? AssistantService.SourceModel : AssistantService.SourceRules, Quota(await store.Used(actor, Today(), Kinds, token)), ResidentAssistantTools.All);
    }

    public async Task<IReadOnlyList<AssistantPromptView>> Prompts(CancellationToken token)
    {
        await Gate(token);
        return (await store.Prompts(false, token)).Where(p => ResidentAssistantTools.All.Contains(p.ToolKey)).ToArray();
    }

    public async Task<AssistantChatResponse> Chat(ResidentChatRequest request, CancellationToken token)
    {
        var actor = await Gate(token);
        var locale = request.Locale ?? "tr"; var message = request.Message?.Trim(); var code = request.PromptCode?.Trim();
        if (request.PropertyId == Guid.Empty || request.UnitId == Guid.Empty || request.Role is null || !ResidentAssistantTools.Roles.Contains(request.Role) || locale is not ("tr" or "en")
            || (code is null) == (message is null) || code is not null && !CodePattern.IsMatch(code)
            || message is not null && (message.Length is 0 or > AssistantService.MaxMessageLength || message.Any(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t')))
            Invalid("message");
        // The mobile finance store rejects any context that is not this caller's own unit before a quota slot is used.
        var balance = await finance.ReadAsync(request.PropertyId, request.UnitId, request.Role!, "charges", 1, null, token);
        AssistantPromptView? prompt = null;
        if (code is not null && ((prompt = await store.ActivePrompt(code, token)) is null || !ResidentAssistantTools.All.Contains(prompt.ToolKey))) Invalid("promptCode");
        var day = Today();
        var audit = await store.Reserve(actor, request.PropertyId, day, settings.DailyLimit, prompt is null ? "RESIDENT_FREE" : "RESIDENT_PROMPT", prompt?.Code, Kinds, token) ?? throw new AssistantBudgetException();
        var started = clock.GetTimestamp(); string? tool = prompt?.ToolKey; var source = AssistantService.SourceRules; var fallback = false; var outcome = "FAILED";
        try
        {
            var question = prompt is null ? message! : locale == "en" ? prompt.TextEn : prompt.TextTr;
            if (tool is null && model.Enabled)
            {
                try { tool = AssistantService.ParseTool(await model.Complete(AssistantService.ActorRef(actor), AssistantService.SelectorSystem(ResidentAssistantTools.All), "<soru>\n" + question + "\n</soru>", token), ResidentAssistantTools.All); source = AssistantService.SourceModel; }
                catch (AssistantModelUnavailableException) { fallback = true; }
            }
            if (tool is null)
            {
                outcome = "UNSUPPORTED";
                return new("UNSUPPORTED", null, source, fallback, AssistantService.Unsupported(locale), [], Quota(await store.Used(actor, day, Kinds, token)));
            }
            var result = tool switch
            {
                ResidentAssistantTools.Balance => await Balance(request, balance, day, token),
                ResidentAssistantTools.Requests => await Requests(request, day, locale, token),
                _ => await Notices(request, day, token)
            };
            var answer = Rules(result, locale); source = AssistantService.SourceRules;
            if (model.Enabled && !fallback)
            {
                try
                {
                    answer = AssistantService.Resolve(AssistantService.ParseAnswer(await model.Complete(AssistantService.ActorRef(actor), AssistantService.AnswerSystem(locale), "<soru>\n" + question + "\n</soru>\n<veri>\n" + AssistantService.DataBlock(result) + "\n</veri>", token), result), result);
                    source = AssistantService.SourceModel;
                }
                catch (AssistantModelUnavailableException) { fallback = true; }
            }
            outcome = "ANSWERED";
            var cards = result.Facts.Select(f => new AssistantSourceCard(result.Tool, f.Key, f.Value, f.Kind, f.Dimension, f.From ?? result.From, f.To ?? result.To, f.Href ?? result.Href, f.Ref, f.Label)).ToArray();
            return new("ANSWERED", tool, source, fallback, answer, cards, Quota(await store.Used(actor, day, Kinds, token)));
        }
        finally
        {
            await store.Finish(audit, tool, outcome == "FAILED" ? null : source, outcome, (int)clock.GetElapsedTime(started).TotalMilliseconds, CancellationToken.None);
        }
    }

    private static void Invalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.AssistantQuestionInvalid.Create()] });
    private static decimal Amount(string value) => decimal.Parse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    private static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // Property and unit names, party names and charge identifiers stay out: only amounts and dates for this caller's own unit.
    private async Task<AssistantToolResult> Balance(ResidentChatRequest r, FinanceSnapshot charges, DateOnly today, CancellationToken token)
    {
        var payments = await finance.ReadAsync(r.PropertyId, r.UnitId, r.Role!, "payments", 1, null, token);
        var s = charges.Summary;
        var facts = new List<AssistantFact> { new("myBalance", s.Balance, "MONEY"), new("myOverdue", s.Overdue, "MONEY", To: today), new("myPaidTotal", s.Paid, "MONEY"), new("myPenalty", s.Penalty, "MONEY") };
        var next = charges.Charges.Select(c => c.Charge).Where(c => Amount(c.Balance) > 0 && c.DueDate >= today).OrderBy(c => c.DueDate).FirstOrDefault();
        if (next is not null) { facts.Add(new("myNextDueDate", Date(next.DueDate), "DATE")); facts.Add(new("myNextDueAmount", next.Balance, "MONEY", next.Period, next.DueDate, next.DueDate)); }
        facts.AddRange(payments.Payments.Where(p => !p.Cancelled).OrderByDescending(p => p.PaidDate).Take(3).Select(p => new AssistantFact("myPayment", p.Amount, "MONEY", p.Period, p.PaidDate, p.PaidDate)));
        return new(ResidentAssistantTools.Balance, today, today, "/finance", facts);
    }

    // Request descriptions and management replies are never read here; only category, status and last change date.
    private async Task<AssistantToolResult> Requests(ResidentChatRequest r, DateOnly today, string locale, CancellationToken token)
    {
        var rows = (await requests.PullAsync(r.PropertyId, r.UnitId, r.Role!, token)).Records.Where(x => !x.Deleted).ToArray();
        var facts = new[] { "RECORDED", "IN_PROGRESS", "RESOLVED", "REJECTED" }.Select(status => new AssistantFact("myRequestsByStatus", Count(rows.Count(x => x.ManagementStatus == status)), "COUNT", status)).ToList();
        facts.AddRange(rows.OrderByDescending(x => x.ModifiedAt).Take(Listed).Select((x, i) =>
            new AssistantFact("myRequestUpdated", Count(today.DayNumber - Day(x.ModifiedAt).DayNumber), "DAYS", x.Category, Day(x.ModifiedAt), today, "R" + (i + 1), StatusLabel(x.ManagementStatus, locale))));
        return new(ResidentAssistantTools.Requests, today, today, "/requests", facts);
    }

    private static string StatusLabel(string status, string locale) => (status, locale == "en") switch
    {
        ("RECORDED", false) => "kaydedilen talebiniz", ("IN_PROGRESS", false) => "işlemdeki talebiniz", ("RESOLVED", false) => "çözülen talebiniz", ("REJECTED", false) => "reddedilen talebiniz",
        ("RECORDED", true) => "your recorded request", ("IN_PROGRESS", true) => "your request in progress", ("RESOLVED", true) => "your resolved request", _ => "your rejected request"
    };

    // Only published announcements reach a resident through the notice store; their text is masked and shortened before the model sees it.
    // Document bodies are never read: only publication dates of documents this caller may list.
    private async Task<AssistantToolResult> Notices(ResidentChatRequest r, DateOnly today, CancellationToken token)
    {
        var page = await notices.ReadAsync(new(r.PropertyId, r.UnitId, r.Role!, "ANNOUNCEMENT", 1), token);
        var facts = new List<AssistantFact> { new("announcementCount", Count(page.Total), "COUNT"), new("unreadAnnouncements", Count(page.Unread), "COUNT") };
        facts.AddRange(page.Items.OrderByDescending(n => n.CreatedAt).Take(Listed).Select((n, i) =>
        {
            var text = AssistantService.Mask(n.Title + "\n" + n.Body, "tr").Text;
            return new AssistantFact("announcementDate", Date(Day(n.CreatedAt)), "DATE", null, null, null, "N" + (i + 1), n.Title, null, text.Length > ExcerptLength ? text[..ExcerptLength] : text);
        }));
        try
        {
            if (await communication.Documents(r.PropertyId, new ContentQuery(1, Listed), token) is { } documents)
                facts.AddRange(documents.Items.Select((d, i) => new AssistantFact("documentDate", Date(Day(d.CreatedAt)), "DATE", null, null, null, "B" + (i + 1), d.FileName)));
        }
        catch (UnauthorizedAccessException) { }
        return new(ResidentAssistantTools.Notices, today, today, "/announcements", facts);
    }

    public static string Rules(AssistantToolResult result, string locale)
    {
        var en = locale == "en"; var culture = CultureInfo.GetCultureInfo(en ? "en-GB" : "tr-TR");
        string? Raw(string key, string? dimension = null) => result.Facts.FirstOrDefault(f => f.Key == key && (dimension is null || f.Dimension == dimension))?.Value;
        string M(string? value) => Amount(value ?? "0").ToString("N2", culture) + (en ? " TRY" : " TL");
        string C(string key, string? dimension = null) => int.Parse(Raw(key, dimension) ?? "0", CultureInfo.InvariantCulture).ToString("N0", culture);
        string D(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("d MMMM yyyy", culture);
        switch (result.Tool)
        {
            case ResidentAssistantTools.Balance:
            {
                var text = en ? $"Your balance for this unit is {M(Raw("myBalance"))}; {M(Raw("myOverdue"))} of it is overdue." : $"Bu bölüm için toplam borcunuz {M(Raw("myBalance"))}; bunun {M(Raw("myOverdue"))} kadarı gecikmede.";
                if (Raw("myNextDueDate") is { } next) text += en ? $" Your next payment of {M(Raw("myNextDueAmount"))} is due on {D(next)}." : $" Bir sonraki ödemeniz {D(next)} tarihinde, {M(Raw("myNextDueAmount"))}.";
                if (result.Facts.FirstOrDefault(f => f.Key == "myPayment") is { } paid) text += en ? $" Your last payment was {M(paid.Value)} on {D(Date(paid.From!.Value))}." : $" Son ödemeniz {D(Date(paid.From!.Value))} tarihinde {M(paid.Value)}.";
                return text;
            }
            case ResidentAssistantTools.Requests:
                return en ? $"You have {C("myRequestsByStatus", "RECORDED")} recorded, {C("myRequestsByStatus", "IN_PROGRESS")} in-progress, {C("myRequestsByStatus", "RESOLVED")} resolved and {C("myRequestsByStatus", "REJECTED")} rejected requests from this unit."
                          : $"Bu bölümden açtığınız {C("myRequestsByStatus", "RECORDED")} kayıtlı, {C("myRequestsByStatus", "IN_PROGRESS")} işlemde, {C("myRequestsByStatus", "RESOLVED")} çözülmüş ve {C("myRequestsByStatus", "REJECTED")} reddedilmiş talebiniz var.";
            default:
            {
                var text = en ? $"There are {C("announcementCount")} announcements; {C("unreadAnnouncements")} are unread." : $"{C("announcementCount")} duyuru var; {C("unreadAnnouncements")} tanesini okumadınız.";
                if (result.Facts.FirstOrDefault(f => f.Key == "announcementDate") is { } latest) text += en ? $" The latest is \"{latest.Label}\" ({D(latest.Value)})." : $" En sonuncusu \"{latest.Label}\" ({D(latest.Value)}).";
                return text;
            }
        }
    }
}
