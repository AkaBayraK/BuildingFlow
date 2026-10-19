using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class ManagementAssignmentTests
{
    private static ManagementAssignment Assignment(DateOnly? end=null)=>new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"MANAGER",new(2026,1,1),end);
    [Fact]public void ScopeRoleDatesAndReasonAreValidated()
    {
        Assert.Throws<ArgumentException>(()=>new ManagementAssignment(Guid.Empty,Guid.NewGuid(),Guid.NewGuid(),"MANAGER",new(2026,1,1)));
        Assert.Throws<ArgumentException>(()=>new ManagementAssignment(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"OWNER",new(2026,1,1)));
        Assert.Throws<ArgumentException>(()=>Assignment(new(2025,12,31)));Assert.Throws<ArgumentException>(()=>ManagementAssignment.CleanReason("bad\nreason"));
    }
    [Fact]public void DatesAreInclusiveAndEndingOncePreservesHistory()
    {
        var a=Assignment();a.End(new(2026,1,2),"  Term ended  ");Assert.Equal("Term ended",a.EndReason);Assert.True(a.Contains(new(2026,1,2)));Assert.False(a.Contains(new(2026,1,3)));Assert.Throws<InvalidOperationException>(()=>a.End(new(2026,1,3),"Again"));
    }
    [Fact]public void CancellationRevokesWithoutRewritingDatesOrOtherRoles()
    {
        var a=Assignment();var before=a.StartDate;a.Cancel(DateTimeOffset.Parse("2026-01-01T12:00:00Z"),"Revoked");Assert.Equal(before,a.StartDate);Assert.Null(a.EndDate);Assert.False(a.Contains(before));Assert.Throws<InvalidOperationException>(()=>a.Cancel(DateTimeOffset.UtcNow,"Again"));Assert.Throws<InvalidOperationException>(()=>a.End(before,"Again"));
    }
}
