using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
// Icon request types: every listed category is accepted, unknown codes are rejected.
public sealed class RequestCategoryTests
{
    [Fact] public void IconCategoriesAreAcceptedAndUnknownCodesRejected()
    {
        Assert.Equal(new[]{"ELEVATOR","WATER","ELECTRICITY","HEATING","RENOVATION","FAULT","CLEANING","SECURITY","COMMON_AREA","SUGGESTION","OTHER"},MobileRequest.Categories);
        var r=new MobileRequest(Guid.NewGuid(),Guid.NewGuid(),null,"RESIDENT","user","Asansör çalışmıyor");
        foreach(var c in MobileRequest.Categories){r.Details(c,"NORMAL",null);Assert.Equal(c,r.Category);}
        Assert.All(MobileRequest.Categories,c=>Assert.True(c.Length<=20));
        Assert.Throws<ArgumentException>(()=>r.Details("PLUMBING","NORMAL",null));
    }
}
