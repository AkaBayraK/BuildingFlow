using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
// Planlama & Bakımlar: trimmed fields, optional texts as null, enum/date bounds; announcement categories edit only drafts.
public sealed class MaintenancePlanTests
{
    [Fact] public void FieldsAreTrimmedAndBlankOptionalsBecomeNull()
    {
        var p=new MaintenancePlan(Guid.NewGuid()," Jeneratör Bakımı ","  ",MaintenanceType.MAINTENANCE,new(2026,11,12)," Jeneratör Odası ","",MaintenanceStatus.PLANNED);
        Assert.Equal("Jeneratör Bakımı",p.Title);Assert.Null(p.Description);Assert.Equal("Jeneratör Odası",p.Location);Assert.Null(p.Responsible);
        p.Edit("Yangın Tatbikatı","Tüm bloklar",MaintenanceType.DRILL,new(2026,11,28),null,"Yönetim",MaintenanceStatus.COMPLETED);
        Assert.Equal(MaintenanceType.DRILL,p.Type);Assert.Equal(MaintenanceStatus.COMPLETED,p.Status);Assert.Equal("Yönetim",p.Responsible);
    }
    [Fact] public void InvalidValuesAreRejected()
    {
        var property=Guid.NewGuid();var date=new DateOnly(2026,11,1);
        Assert.Throws<ArgumentException>(()=>new MaintenancePlan(Guid.Empty,"T",null,MaintenanceType.OTHER,date,null,null,MaintenanceStatus.PLANNED));
        Assert.ThrowsAny<ArgumentException>(()=>new MaintenancePlan(property," ",null,MaintenanceType.OTHER,date,null,null,MaintenanceStatus.PLANNED));
        Assert.ThrowsAny<ArgumentException>(()=>new MaintenancePlan(property,new string('x',201),null,MaintenanceType.OTHER,date,null,null,MaintenanceStatus.PLANNED));
        Assert.ThrowsAny<ArgumentException>(()=>new MaintenancePlan(property,"T",new string('x',2001),MaintenanceType.OTHER,date,null,null,MaintenanceStatus.PLANNED));
        Assert.ThrowsAny<ArgumentException>(()=>new MaintenancePlan(property,"T",null,(MaintenanceType)99,date,null,null,MaintenanceStatus.PLANNED));
        Assert.ThrowsAny<ArgumentException>(()=>new MaintenancePlan(property,"T",null,MaintenanceType.OTHER,date,null,null,(MaintenanceStatus)99));
        Assert.ThrowsAny<ArgumentException>(()=>new MaintenancePlan(property,"T",null,MaintenanceType.OTHER,new(1999,12,31),null,null,MaintenanceStatus.PLANNED));
        Assert.ThrowsAny<ArgumentException>(()=>new MaintenancePlan(property,"T",null,MaintenanceType.OTHER,new(2101,1,1),null,null,MaintenanceStatus.PLANNED));
    }
    [Fact] public void AnnouncementCategoryDefaultsToGeneralAndIsLockedAfterPublish()
    {
        Assert.Equal(new[]{"GENERAL","MEETING","DUES","MAINTENANCE","URGENT","OTHER"},Enum.GetNames<AnnouncementCategory>());
        var a=new Announcement(Guid.NewGuid(),"Aidat","İçerik",ContentAudience.ALL_MEMBERS);Assert.Equal(AnnouncementCategory.GENERAL,a.Category);
        a.Edit("Aidat","İçerik",ContentAudience.ALL_MEMBERS,AnnouncementCategory.DUES);Assert.Equal(AnnouncementCategory.DUES,a.Category);
        a.Edit("Aidat 2","İçerik",ContentAudience.ALL_MEMBERS);Assert.Equal(AnnouncementCategory.DUES,a.Category);
        Assert.Throws<ArgumentException>(()=>a.Edit("x","y",ContentAudience.ALL_MEMBERS,(AnnouncementCategory)42));
        a.Publish(DateTimeOffset.UtcNow);Assert.Throws<InvalidOperationException>(()=>a.Edit("x","y",ContentAudience.ALL_MEMBERS,AnnouncementCategory.URGENT));
    }
}
