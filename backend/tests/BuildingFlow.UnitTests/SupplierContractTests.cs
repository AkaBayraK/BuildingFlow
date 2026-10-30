using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class SupplierContractTests
{
    [Fact] public void DraftCanChangeButActivatedContentAndCancellationAreImmutable()
    {
        var contract=new SupplierContract(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"Annual",new(2026,1,1),new(2026,12,31),1000m,30);
        contract.Edit("Revised",contract.StartDate,contract.EndDate,1200m,10,null);contract.Activate();
        Assert.Throws<InvalidOperationException>(()=>contract.Edit("Overwrite",contract.StartDate,contract.EndDate,1m,0,null));
        Assert.Throws<InvalidOperationException>(()=>contract.Activate());contract.Cancel("Terminated by management");Assert.Equal(SupplierContractStatus.CANCELLED,contract.Status);
        Assert.Throws<InvalidOperationException>(()=>contract.Cancel("Again"));
    }
    [Theory][InlineData(0)][InlineData(1.001)][InlineData(-1)] public void ContractAmountsRemainPositiveTwoDecimals(decimal amount)
    {Assert.ThrowsAny<ArgumentException>(()=>new SupplierContract(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"Annual",new(2026,1,1),new(2026,12,31),amount,30));}
    [Fact] public void SupplierFilesCannotUsePublicAudienceOrAnnouncementAttachment()
    {
        var p=Guid.NewGuid();var hash=new string('A',64);
        Assert.Throws<ArgumentException>(()=>new StoredDocument(p,Guid.NewGuid(),hash,null,ContentAudience.ALL_MEMBERS,"file.pdf","application/pdf",1,hash,true));
        Assert.Throws<ArgumentException>(()=>new StoredDocument(p,Guid.NewGuid(),hash,Guid.NewGuid(),ContentAudience.MANAGEMENT,"file.pdf","application/pdf",1,hash,true));
    }
}
