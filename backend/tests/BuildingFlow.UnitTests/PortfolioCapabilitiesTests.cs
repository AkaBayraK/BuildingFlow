using BuildingFlow.Application;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class PortfolioCapabilitiesTests
{
    [Fact]public void UnitCapacitiesNeverGrantManagementAndLeaseFollowsTenancyOrRentedOut()
    {
        var owner=PortfolioCapabilities.ForUnit(["OWNER"],PersonPortfolio.OwnerOnly);Assert.Contains(PortfolioCapabilities.ViewDues,owner);Assert.DoesNotContain(PortfolioCapabilities.ViewLease,owner);
        Assert.Contains(PortfolioCapabilities.ViewLease,PortfolioCapabilities.ForUnit(["OWNER"],PersonPortfolio.RentedOut));
        var resident=PortfolioCapabilities.ForUnit(["RESIDENT"],PersonPortfolio.ResidentOnly);Assert.Equal([PortfolioCapabilities.AnswerSurvey,PortfolioCapabilities.CreateRequest,PortfolioCapabilities.ViewAnnouncements,PortfolioCapabilities.ViewOwnRelations],resident);
        var both=PortfolioCapabilities.ForUnit(["TENANT","RESIDENT"],PersonPortfolio.TenantResident);Assert.Equal(both.Distinct().Count(),both.Length);Assert.Contains(PortfolioCapabilities.ViewLease,both);
        Assert.All(PortfolioCapabilities.ForUnit(["OWNER","TENANT","RESIDENT"],PersonPortfolio.OwnerOccupied),c=>Assert.DoesNotContain("MANAGE",c));
        Assert.Throws<ArgumentException>(()=>PortfolioCapabilities.ForUnit(["MANAGER"],""));
    }
    [Fact]public void WorkspaceCapabilitiesMirrorServerFlagsAndContextKeyIsStable()
    {
        var id=Guid.NewGuid();var tenant=Guid.NewGuid();var context=new SessionContext(id,"MANAGEMENT",tenant,"T",Guid.NewGuid(),"P",null,null,"MANAGER",null,null,true,true,false);
        Assert.Equal([PortfolioCapabilities.ManagePeople,PortfolioCapabilities.ManageFinance],PortfolioCapabilities.ForWorkspace(context));
        Assert.Equal($"{tenant:D}:TENANT:{id:D}",PortfolioCapabilities.ContextKey(tenant,"TENANT",id));
    }
}
