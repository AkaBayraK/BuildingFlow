namespace BuildingFlow.Application;
public sealed record PortfolioContextRef(string Capacity,Guid RelationId,string ContextKey);
public sealed record PortfolioUnitAccess(Guid TenantId,Guid PropertyId,Guid UnitId,string Status,IReadOnlyList<string> Capabilities,IReadOnlyList<PortfolioContextRef> Contexts);
public sealed record PortfolioPerson(Guid TenantId,string TenantName,Guid PersonId,string DisplayName,IReadOnlyList<PortfolioProperty> Properties);
public sealed record MyPortfolio(DateOnly AsOf,string Fingerprint,IReadOnlyList<PortfolioPerson> People,IReadOnlyList<PortfolioUnitAccess> Units,IReadOnlyList<SessionContext> Workspaces);
public sealed record ActiveContextQuery(Guid PropertyId,Guid? UnitId,string Capacity,Guid RelationId);
public sealed record ActiveContext(SessionContext Context,string ContextKey,IReadOnlyList<string> Capabilities);
public interface IMyPortfolioStore
{
    Task<MyPortfolio> PortfolioAsync(CancellationToken token);
    Task<ActiveContext?> ResolveAsync(ActiveContextQuery query,CancellationToken token);
}
// Capabilities describe what the context may request; every module still re-checks the context on the server.
public static class PortfolioCapabilities
{
    public const string ViewDues="VIEW_DUES",ViewPayments="VIEW_PAYMENTS",ViewAnnouncements="VIEW_ANNOUNCEMENTS",ViewDocuments="VIEW_DOCUMENTS",CreateRequest="CREATE_REQUEST",AnswerSurvey="ANSWER_SURVEY",ViewLease="VIEW_LEASE",ViewOwnRelations="VIEW_OWN_RELATIONS",
        ManagePeople="MANAGE_PEOPLE",ManageFinance="MANAGE_FINANCE",ManageAssignments="MANAGE_ASSIGNMENTS";
    private static readonly IReadOnlyDictionary<string,string[]> ByCapacity=new Dictionary<string,string[]>
    {
        ["OWNER"]=[ViewDues,ViewPayments,ViewAnnouncements,ViewDocuments,CreateRequest,AnswerSurvey,ViewOwnRelations],
        ["TENANT"]=[ViewDues,ViewPayments,ViewAnnouncements,ViewDocuments,CreateRequest,AnswerSurvey,ViewOwnRelations,ViewLease],
        ["RESIDENT"]=[ViewAnnouncements,CreateRequest,AnswerSurvey,ViewOwnRelations],
    };
    public static string[] ForUnit(IEnumerable<string> capacities,string status)
    {
        var set=capacities.SelectMany(c=>ByCapacity.TryGetValue(c,out var values)?values:throw new ArgumentException("Invalid capacity.")).ToHashSet();
        if(status==PersonPortfolio.RentedOut)set.Add(ViewLease);
        return set.Order(StringComparer.Ordinal).ToArray();
    }
    public static string[] ForWorkspace(SessionContext context)=>new[]{context.CanManagePeople?ManagePeople:null,context.CanManageFinance?ManageFinance:null,context.CanManageAssignments?ManageAssignments:null}.OfType<string>().ToArray();
    public static string ContextKey(Guid tenant,string kind,Guid id)=>$"{tenant:D}:{kind}:{id:D}";
}
