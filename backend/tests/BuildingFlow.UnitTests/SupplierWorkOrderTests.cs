using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class SupplierWorkOrderTests
{
    private static readonly DateTimeOffset Now = new(2027, 1, 1, 9, 0, 0, TimeSpan.Zero);
    private static SupplierWorkOrder Order(DateTimeOffset? due = null) => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), SupplierWorkOrderSource.REQUEST, Guid.NewGuid(), 1, "Original request\nSecond line", "Repair", "Check and repair\nProvide evidence", due ?? Now.AddHours(24));
    [Fact] public void LifecycleRequiresEveryStepKeepsCompletionAndFinalStatesImmutable()
    {
        var order = Order(); var time = Now;
        foreach (var next in new[] { SupplierWorkOrderStatus.ASSIGNED, SupplierWorkOrderStatus.ACCEPTED, SupplierWorkOrderStatus.IN_PROGRESS, SupplierWorkOrderStatus.COMPLETED, SupplierWorkOrderStatus.VERIFIED, SupplierWorkOrderStatus.CLOSED })
        { order.Transition(next, next == SupplierWorkOrderStatus.COMPLETED ? "Completed with evidence" : "Recorded by authorized manager", time); time = time.AddMinutes(1); }
        Assert.Equal("Completed with evidence", order.CompletionNote); Assert.Equal(Now.AddMinutes(3), order.CompletedAt); Assert.Equal(Now.AddMinutes(5), order.ClosedAt); Assert.False(order.AllowsPhotos);
        Assert.Throws<InvalidOperationException>(() => order.Edit("Overwrite", null));
        Assert.Throws<InvalidOperationException>(() => order.Transition(SupplierWorkOrderStatus.CANCELLED, "After closure", time));
        Assert.Null(order.Amount); Assert.Null(order.Currency);
    }
    [Theory] [InlineData(SupplierWorkOrderStatus.ACCEPTED)] [InlineData(SupplierWorkOrderStatus.IN_PROGRESS)] [InlineData(SupplierWorkOrderStatus.COMPLETED)] [InlineData(SupplierWorkOrderStatus.VERIFIED)] [InlineData(SupplierWorkOrderStatus.CLOSED)] [InlineData(SupplierWorkOrderStatus.REJECTED)]
    public void DraftCannotSkipAssignment(SupplierWorkOrderStatus target) => Assert.Throws<InvalidOperationException>(() => Order().Transition(target, "Skip", Now));
    [Fact] public void AssignmentRequiresFutureSlaAndValidationIsAtomic()
    {
        var order = Order(); order.Edit("Draft scope", null);
        Assert.Throws<ArgumentException>(() => order.Transition(SupplierWorkOrderStatus.ASSIGNED, "Assign", Now)); Assert.Equal(SupplierWorkOrderStatus.DRAFT, order.Status); Assert.Null(order.AssignedAt);
        order.Edit("Draft scope", Now); Assert.Throws<ArgumentException>(() => order.Transition(SupplierWorkOrderStatus.ASSIGNED, "Assign", Now));
        order.Edit("Draft scope", Now.AddHours(1)); order.Transition(SupplierWorkOrderStatus.ASSIGNED, "Assign", Now);
        Assert.Throws<ArgumentException>(() => order.Transition(SupplierWorkOrderStatus.ACCEPTED, "Accept", Now.AddSeconds(-1))); Assert.Null(order.AcceptedAt);
    }
    [Fact] public void RejectionAndCancellationRequireReasonsAndKeepHistory()
    {
        var order = Order(); Assert.Throws<ArgumentException>(() => order.Transition(SupplierWorkOrderStatus.CANCELLED, " ", Now)); Assert.Equal(SupplierWorkOrderStatus.DRAFT, order.Status);
        order.Transition(SupplierWorkOrderStatus.ASSIGNED, "Assign", Now); order.Transition(SupplierWorkOrderStatus.REJECTED, "Supplier declined scope", Now.AddMinutes(1));
        Assert.Equal("Supplier declined scope", order.DecisionReason); Assert.NotNull(order.AssignedAt); Assert.False(order.AllowsPhotos);
        Assert.Throws<InvalidOperationException>(() => order.Transition(SupplierWorkOrderStatus.ASSIGNED, "Retry", Now.AddHours(1)));
    }
    [Fact] public void ScopeAndPhotoLinksCannotCrossSupplierOrAreaOrContinueAfterCompletion()
    {
        var order = Order(); var wrong = new SupplierDocument(order.TenantId, Guid.NewGuid(), order.PropertyId, Guid.NewGuid(), null);
        Assert.Throws<InvalidOperationException>(() => new SupplierWorkOrderPhoto(order, wrong, "Photo"));
        var local = new SupplierDocument(order.TenantId, order.SupplierId, order.PropertyId, Guid.NewGuid(), null); Assert.Equal(local.Id, new SupplierWorkOrderPhoto(order, local, "Before repair").DocumentId);
        foreach (var status in new[] { SupplierWorkOrderStatus.ASSIGNED, SupplierWorkOrderStatus.ACCEPTED, SupplierWorkOrderStatus.IN_PROGRESS, SupplierWorkOrderStatus.COMPLETED }) order.Transition(status, "Step evidence", Now);
        Assert.Throws<InvalidOperationException>(() => new SupplierWorkOrderPhoto(order, local, "After completion"));
    }
    [Fact] public void SourceMustBePersistedRequestOrPlanAndNeverImplicitlyPriced()
    {
        Assert.Throws<ArgumentException>(() => new SupplierWorkOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), SupplierWorkOrderSource.QUOTE, Guid.NewGuid(), 1, "Snapshot", "Title", "Scope", null));
        Assert.Throws<ArgumentException>(() => new SupplierWorkOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), SupplierWorkOrderSource.REQUEST, Guid.NewGuid(), 0, "Snapshot", "Title", "Scope", null));
        var order = Order(); Assert.NotNull(order.RequestId); Assert.Null(order.QuoteId); Assert.Null(order.MaintenancePlanId); Assert.Equal(1, order.SourceVersion);
    }
}
