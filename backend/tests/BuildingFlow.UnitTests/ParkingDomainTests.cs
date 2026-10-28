using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
// Araçlar/Otopark: plate normalization, approval lifecycle, quotas, allocation dates and guest windows.
public sealed class ParkingDomainTests
{
    private static readonly Guid P = Guid.NewGuid();
    private static readonly DateTimeOffset T = new(2026, 10, 27, 9, 0, 0, TimeSpan.FromHours(3));

    [Theory]
    [InlineData(" 34 abc 123 ", "34ABC123")]
    [InlineData("06-DEF-456", "06DEF456")]
    [InlineData("b.ab.1234", "BAB1234")]
    public void PlateIsNormalized(string input, string expected) => Assert.Equal(expected, VehiclePlate.Normalize(input));

    [Theory]
    [InlineData(" ")]
    [InlineData("A")]
    [InlineData("34ÇAB12")]
    [InlineData("34ABC123456789")]
    [InlineData("34_ABC")]
    public void InvalidPlateIsRejected(string input) => Assert.ThrowsAny<ArgumentException>(() => VehiclePlate.Normalize(input));

    [Fact]
    public void VehicleStartsPendingAndIsReviewedOnce()
    {
        var vehicle = NewVehicle();
        Assert.Equal((VehicleStatus.PENDING, "34ABC123", "BMW", (string?)null), (vehicle.Status, vehicle.Plate, vehicle.Brand, vehicle.Color));
        vehicle.Approve(T);
        Assert.Equal((VehicleStatus.APPROVED, T), (vehicle.Status, vehicle.ReviewedAt));
        Assert.Throws<InvalidOperationException>(() => vehicle.Reject(T, "Tekrar"));
        vehicle.Deactivate(T.AddDays(1));
        Assert.False(vehicle.IsActive);
        Assert.Throws<InvalidOperationException>(() => vehicle.Edit(VehicleType.CAR, null, null, null));
        Assert.Throws<InvalidOperationException>(() => vehicle.Deactivate(T.AddDays(2)));
    }

    [Fact]
    public void RejectionRequiresReason()
    {
        var vehicle = NewVehicle();
        Assert.ThrowsAny<ArgumentException>(() => vehicle.Reject(T, " "));
        vehicle.Reject(T, " Ruhsat eksik ");
        Assert.Equal((VehicleStatus.REJECTED, "Ruhsat eksik"), (vehicle.Status, vehicle.ReviewReason));
        Assert.Throws<InvalidOperationException>(() => vehicle.Approve(T));
    }

    [Fact]
    public void VehicleScopeAndTextLimitsAreEnforced()
    {
        Assert.Throws<ArgumentException>(() => new Vehicle(P, Guid.Empty, Guid.NewGuid(), "34ABC1", VehicleType.CAR, null, null, null));
        Assert.Throws<ArgumentException>(() => new Vehicle(P, Guid.NewGuid(), Guid.NewGuid(), "34ABC1", (VehicleType)99, null, null, null));
        Assert.ThrowsAny<ArgumentException>(() => new Vehicle(P, Guid.NewGuid(), Guid.NewGuid(), "34ABC1", VehicleType.CAR, new string('x', 51), null, null));
    }

    [Fact]
    public void PolicyEnforcesQuotasAndGuestWindow()
    {
        var policy = new ParkingPolicy(P, 2, 1, 1, 24);
        Assert.Equal(ParkingPolicy.DefaultRetentionDays, policy.RetentionDays);
        Assert.True(policy.AllowsVehicle(1, 0));
        Assert.False(policy.AllowsVehicle(2, 0));
        Assert.False(policy.AllowsVehicle(0, 1));
        Assert.True(policy.AllowsSpace(0));
        Assert.False(policy.AllowsSpace(1));
        Assert.True(policy.AllowsGuestWindow(T, T.AddHours(24)));
        Assert.False(policy.AllowsGuestWindow(T, T.AddHours(25)));
        Assert.False(policy.AllowsGuestWindow(T, T));
        Assert.ThrowsAny<ArgumentException>(() => new ParkingPolicy(P, 0, 1, 1, 24));
        Assert.ThrowsAny<ArgumentException>(() => new ParkingPolicy(P, 1, 1, -1, 24));
        Assert.ThrowsAny<ArgumentException>(() => new ParkingPolicy(P, 1, 1, 1, 169));
        Assert.ThrowsAny<ArgumentException>(() => new ParkingPolicy(P, 1, 1, 1, 24, 366));
    }

    [Fact]
    public void FacilityLevelAndSpaceValidateScope()
    {
        var shared = new ParkingFacility(P, null, " Kapalı Otopark ");
        Assert.True(shared.IsShared);
        Assert.Equal("Kapalı Otopark", shared.Name);
        Assert.Throws<ArgumentException>(() => new ParkingFacility(P, Guid.Empty, "A"));
        Assert.ThrowsAny<ArgumentException>(() => new ParkingLevel(P, shared.Id, "B1", 51));
        var space = new ParkingSpace(P, shared.Id, null, " a01 ", ParkingSpaceType.ACCESSIBLE);
        Assert.Equal("A01", space.Code);
        Assert.True(space.IsAllocatable);
        Assert.False(new ParkingSpace(P, shared.Id, null, "M1", ParkingSpaceType.VISITOR).IsAllocatable);
        Assert.Throws<ArgumentException>(() => new ParkingSpace(P, shared.Id, Guid.Empty, "A2", ParkingSpaceType.STANDARD));
    }

    [Fact]
    public void AllocationRejectsVisitorOrForeignSpaceAndKeepsHistory()
    {
        var facility = Guid.NewGuid();
        var space = new ParkingSpace(P, facility, null, "A01", ParkingSpaceType.STANDARD);
        Assert.Throws<ArgumentException>(() => new ParkingAllocation(P, new ParkingSpace(P, facility, null, "M1", ParkingSpaceType.VISITOR), Guid.NewGuid(), null, new(2026, 11, 1)));
        Assert.Throws<ArgumentException>(() => new ParkingAllocation(Guid.NewGuid(), space, Guid.NewGuid(), null, new(2026, 11, 1)));
        Assert.Throws<ArgumentException>(() => new ParkingAllocation(P, space, Guid.NewGuid(), null, new(2026, 11, 2), new(2026, 11, 1)));
        var allocation = new ParkingAllocation(P, space, Guid.NewGuid(), null, new(2026, 11, 1));
        Assert.True(allocation.Overlaps(new(2027, 1, 1), null));
        allocation.End(new(2026, 11, 30));
        Assert.False(allocation.Contains(new(2026, 12, 1)));
        Assert.False(allocation.Overlaps(new(2026, 12, 1), new(2026, 12, 31)));
        Assert.Throws<InvalidOperationException>(() => allocation.End(new(2026, 12, 31)));
    }

    [Fact]
    public void GuestVehicleFollowsPlannedArrivedDepartedFlow()
    {
        var policy = new ParkingPolicy(P, 2, 2, 1, 12);
        Assert.Throws<ArgumentException>(() => new GuestVehicle(policy, Guid.NewGuid(), "34TST100", null, T, T.AddHours(13), null, null));
        var guest = new GuestVehicle(policy, Guid.NewGuid(), "34 tst 100", " Ahmet ", T, T.AddHours(8), null, " ");
        Assert.Equal((P, "34TST100", "Ahmet", (string?)null, GuestVehicleStatus.PLANNED), (guest.PropertyId, guest.Plate, guest.GuestName, guest.Note, guest.Status));
        Assert.True(guest.Overlaps(T.AddHours(7), T.AddHours(9)));
        Assert.False(guest.Overlaps(T.AddHours(8), T.AddHours(9)));
        Assert.Throws<InvalidOperationException>(() => guest.MarkDeparted(T));
        guest.MarkArrived(T.AddMinutes(5));
        Assert.Throws<InvalidOperationException>(() => guest.Cancel(T));
        guest.MarkDeparted(T.AddHours(2));
        Assert.Equal(GuestVehicleStatus.DEPARTED, guest.Status);
        Assert.False(guest.Overlaps(T, T.AddHours(1)));
    }

    [Fact]
    public void CancelledGuestFreesTheWindow()
    {
        var guest = new GuestVehicle(new ParkingPolicy(P, 1, 1, 0, 4), Guid.NewGuid(), "06MIS234", null, T, T.AddHours(4), null, null);
        guest.Cancel(T.AddMinutes(-30));
        Assert.Equal(GuestVehicleStatus.CANCELLED, guest.Status);
        Assert.False(guest.Overlaps(T, T.AddHours(1)));
        Assert.Throws<InvalidOperationException>(() => guest.MarkArrived(T));
    }

    [Fact]
    public void GuestArrivalIsWithinHalfOpenWindow()
    {
        var guest = new GuestVehicle(new ParkingPolicy(P, 1, 1, 0, 4), Guid.NewGuid(), "34TIME1", null, T, T.AddHours(1), null, null);
        Assert.Throws<InvalidOperationException>(() => guest.MarkArrived(T.AddTicks(-1)));
        Assert.Throws<InvalidOperationException>(() => guest.MarkArrived(T.AddHours(1)));
        guest.MarkArrived(T);
        Assert.Equal(T, guest.ArrivedAt);
    }

    [Fact]
    public void MaintenanceClosureKeepsCancellationHistoryAndHalfOpenBounds()
    {
        var closure = new ParkingClosure(P, Guid.NewGuid(), Guid.NewGuid(), T, T.AddHours(1), " Repair ");
        Assert.Equal("Repair", closure.Reason);
        Assert.True(closure.Overlaps(T.AddMinutes(59), T.AddHours(2)));
        Assert.False(closure.Overlaps(T.AddHours(1), T.AddHours(2)));
        Assert.Throws<InvalidOperationException>(() => closure.Cancel(T.AddHours(1), "Done"));
        Assert.ThrowsAny<ArgumentException>(() => closure.Cancel(T, " "));
        closure.Cancel(T, " Done ");
        Assert.Equal("Done", closure.CancellationReason);
        Assert.False(closure.Overlaps(T, T.AddMinutes(10)));
        Assert.Throws<InvalidOperationException>(() => closure.Cancel(T, "Again"));
    }

    private static Vehicle NewVehicle() => new(P, Guid.NewGuid(), Guid.NewGuid(), "34 abc 123", VehicleType.CAR, " BMW ", "5 Serisi", "");
    [Fact]
    public void RetentionPreservesActiveRecordsAndRedactsOnlyAtTerminalCutoff()
    {
        var now = DateTimeOffset.UtcNow; var p = Guid.NewGuid();
        var v = new Vehicle(p, Guid.NewGuid(), Guid.NewGuid(), "34ABC123", VehicleType.CAR, "brand", "model", "color");
        Assert.False(v.Redact(now, 30)); v.Reject(now.AddDays(-30), "private reason");
        Assert.False(v.Redact(now.AddTicks(-1), 30)); Assert.True(v.Redact(now, 30));
        Assert.Equal("REDACTED", v.Plate); Assert.Null(v.Brand); Assert.Equal("REDACTED", v.ReviewReason);
        Assert.False(v.Redact(now, 30)); Assert.Equal(p, v.PropertyId);
        var guest = new GuestVehicle(new ParkingPolicy(p,2,2,1,24),Guid.NewGuid(),"35XYZ123","name",now.AddDays(-31),now.AddDays(-31).AddHours(1),null,"note");
        Assert.False(guest.Redact(now,30)); guest.MarkArrived(now.AddDays(-31));
        Assert.False(guest.Redact(now,30)); guest.MarkDeparted(now.AddDays(-30));
        Assert.True(guest.Redact(now,30)); Assert.Null(guest.GuestName); Assert.Null(guest.Note); Assert.Equal("REDACTED",guest.Plate);
        Assert.Throws<ArgumentOutOfRangeException>(()=>guest.Redact(now,0));
    }
}
