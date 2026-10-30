using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class SupplierTests
{
    [Fact] public void StatusRequiresReasonAndKeepsDisabledHistory()
    {
        var supplier = new Supplier(Guid.NewGuid(), " Acme ", SupplierKind.COMPANY);
        Assert.Equal("Acme", supplier.Name); Assert.Equal("ACME", supplier.NameKey);
        Assert.Throws<ArgumentException>(() => supplier.ChangeStatus(SupplierStatus.BLOCKED, null));
        Assert.Equal(SupplierStatus.DRAFT, supplier.Status);
        supplier.ChangeStatus(SupplierStatus.BLOCKED, " Safety review ");
        Assert.False(supplier.IsEnabled); Assert.Equal("Safety review", supplier.StatusReason);
        supplier.ChangeStatus(SupplierStatus.ACTIVE, null); Assert.True(supplier.IsEnabled);
    }
    [Fact] public void ScopeAndContactAreRequiredAndInvalidEditDoesNotPartiallyApply()
    {
        Assert.Throws<ArgumentException>(() => new Supplier(Guid.Empty, "A", SupplierKind.COMPANY));
        var t = Guid.NewGuid(); var s = new Supplier(t, "A", SupplierKind.COMPANY);
        Assert.Throws<ArgumentException>(() => s.Edit("B", SupplierKind.INDIVIDUAL, new string('x', 2001)));
        Assert.Equal("A", s.Name); Assert.Equal(SupplierKind.COMPANY, s.Kind);
        Assert.Throws<ArgumentException>(() => new SupplierContact(t, s.Id, "Contact", null, " "));
        Assert.Throws<ArgumentException>(() => new SupplierManagementArea(t, s.Id, Guid.Empty));
        Assert.Throws<ArgumentException>(() => new SupplierDocument(t, s.Id, Guid.NewGuid(), Guid.Empty, null));
    }
}
