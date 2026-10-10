using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class PeopleDomainTests
{
    [Fact] public void PersonNormalizesOptionalDataAndPreservesIdentity()
    {
        var property = Guid.NewGuid(); var person = new Person(property, " Ada ", " Kaya ", " ", " ada@example.test ");
        Assert.Equal(property, person.PropertyId); Assert.Equal("Ada", person.FirstName); Assert.Equal("Kaya", person.LastName);
        Assert.Null(person.PhoneNumber); Assert.Equal("ada@example.test", person.Email);
        var id = person.Id; person.Update("Ada", "Yılmaz", "+90 555", null, false); Assert.Equal(id, person.Id); Assert.False(person.IsEnabled);
    }
    [Theory] [InlineData("", "Kaya")] [InlineData("Ada", " ")]
    public void PersonRequiresName(string first, string last) => Assert.Throws<ArgumentException>(() => new Person(Guid.NewGuid(), first, last));
    [Fact] public void FailedPersonEditIsAtomic()
    {
        var person = new Person(Guid.NewGuid(), "Ada", "Kaya");
        Assert.Throws<ArgumentException>(() => person.Update("New", new string('x', 101), null, null, false));
        Assert.Equal("Ada", person.FirstName); Assert.True(person.IsEnabled);
    }
    [Fact] public void DatesAreInclusiveAndNonOverlappingNextDayIsAllowed()
    {
        var start = new DateOnly(2026, 1, 1); var end = start.AddDays(10);
        var link = new UnitOccupancy(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OccupancyType.OWNER, start, end, true);
        Assert.True(link.Contains(start)); Assert.True(link.Contains(end)); Assert.False(link.Contains(end.AddDays(1)));
        Assert.True(link.Overlaps(end, null)); Assert.False(link.Overlaps(end.AddDays(1), null)); Assert.True(link.IsPrimaryContact);
    }
    [Fact] public void EndPreservesRelationshipAndCannotRewriteHistory()
    {
        var start = new DateOnly(2026, 1, 1); var link = new UnitOccupancy(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OccupancyType.TENANT, start);
        var id = link.Id; Assert.Throws<ArgumentException>(() => link.End(start.AddDays(-1))); Assert.Null(link.EndDate);
        link.End(start.AddDays(2)); Assert.Equal(id, link.Id); Assert.Equal(start, link.StartDate); Assert.False(link.IsDeleted);
        Assert.Throws<InvalidOperationException>(() => link.End(start.AddDays(3)));
    }
    [Fact] public void InvalidIdentityRoleAndDateAreRejected()
    {
        var id = Guid.NewGuid(); var day = new DateOnly(2026, 1, 1);
        Assert.Throws<ArgumentException>(() => new Person(Guid.Empty, "A", "B"));
        Assert.Throws<ArgumentException>(() => new UnitOccupancy(id, Guid.Empty, id, OccupancyType.OWNER, day));
        Assert.Throws<ArgumentException>(() => new UnitOccupancy(id, id, id, (OccupancyType)99, day));
        Assert.Throws<ArgumentException>(() => new UnitOccupancy(id, id, id, OccupancyType.OWNER, day, day.AddDays(-1)));
    }
}
