using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
// TNM 01: the registry mirrors the codes the domain already validates; keys are unique and levels/groups bounded.
public sealed class DefinitionRegistryTests
{
    private static readonly DefinitionsResponse Registry=DefinitionRegistry.Current;
    [Fact] public void ListValuesMatchDomainSources()
    {
        string[] Values(string key)=>Registry.Lists.Single(x=>x.Key==key).Values.ToArray();
        Assert.Equal(Enum.GetNames<ExpenseCategory>(),Values("EXPENSE_CATEGORY"));
        Assert.Equal(Enum.GetNames<VehicleType>(),Values("VEHICLE_TYPE"));
        Assert.Equal(MobileRequest.Categories,Values("REQUEST_CATEGORY"));
        Assert.Equal(new[]{"LOW","NORMAL","HIGH"},Values("REQUEST_PRIORITY"));
        Assert.All(Registry.Lists,x=>{Assert.NotEmpty(x.Values);Assert.Equal(x.Values.Count,x.Values.Distinct().Count());});
    }
    [Fact] public void KeysAreUniqueAndClassificationIsBounded()
    {
        var keys=Registry.Lists.Select(x=>x.Key).Concat(Registry.Catalogs.Select(x=>x.Key)).ToArray();
        Assert.Equal(keys.Length,keys.Distinct().Count());
        Assert.All(Registry.Lists,x=>{Assert.True(Enum.IsDefined(Enum.Parse<DefinitionGroup>(x.Group)));Assert.Contains(x.Level,new[]{"FIXED","SYSTEM","PROPERTY"});Assert.Equal(x.Level=="FIXED",x.PlannedPhase is null);});
        Assert.All(Registry.Catalogs,x=>Assert.True(x.Route is null||x.Route.StartsWith('/')));
    }
    [Fact] public void BehaviourBearingListsStayFixed()
    {
        foreach(var key in new[]{"PROPERTY_TYPE","UNIT_TYPE","CHARGE_KIND","PAYMENT_METHOD","PARKING_SPACE_TYPE","SUPPLIER_KIND","MAINTENANCE_FREQUENCY"})
            Assert.Equal("FIXED",Registry.Lists.Single(x=>x.Key==key).Level);
        Assert.Equal("TNM_05",Registry.Lists.Single(x=>x.Key=="EXPENSE_CATEGORY").PlannedPhase);
    }
}
public sealed class DefinitionValueTests
{
    [Fact] public void OverridesTrimBlankToNullAndBoundSortOrder()
    {
        var value=new DefinitionValue("REQUEST_CATEGORY","ELEVATOR");value.Set("  Asansör ","",null," red ",5,false);
        Assert.Equal("Asansör",value.NameTr);Assert.Null(value.NameEn);Assert.Equal("red",value.Tone);Assert.False(value.IsEnabled);
        Assert.Throws<ArgumentOutOfRangeException>(()=>value.Set(null,null,null,null,1000,true));
        Assert.Throws<ArgumentException>(()=>value.Set(new string('a',101),null,null,null,0,true));
    }
    [Fact] public void OnlyTnm02ListsAreEditableAndFallbacksAreLocked()
    {
        var editable=DefinitionRegistry.Current.Lists.Where(DefinitionRules.Editable).Select(x=>x.Key).ToArray();
        Assert.Equal(new[]{"COMMON_AREA_TYPE","REQUEST_CATEGORY","REQUEST_PRIORITY","MAINTENANCE_TYPE","VEHICLE_TYPE","ANNOUNCEMENT_CATEGORY","SURVEY_CATEGORY"},editable);
        Assert.All(DefinitionRegistry.Current.Lists.Where(DefinitionRules.Editable),x=>Assert.Contains(x.Values,DefinitionRules.Locked));
        Assert.All(DefinitionRules.IconKeys,k=>Assert.Matches("^[a-z0-9-]{2,40}$",k));
    }
}
