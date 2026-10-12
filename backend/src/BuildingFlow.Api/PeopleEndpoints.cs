using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class PeopleEndpoints
{
    public static void MapPeopleEndpoints(this WebApplication app)
    {
        // Personal data is available only to management roles, with store-level membership scope.
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}").WithTags("People").RequireAuthorization("CanManagePeople");
        root.MapPost("/people/{personId:guid}/messages",async(Guid propertyId,Guid personId,SendPersonMessageRequest request,PersonMessageService service,CancellationToken token)=>Found(await service.SendAsync(propertyId,personId,request,token)));
        root.MapGet("/people/{personId:guid}/messages",async(Guid propertyId,Guid personId,PersonMessageService service,CancellationToken token,int page=1)=>Found(await service.HistoryAsync(propertyId,personId,page,token)));
        app.MapGet("/api/v1/notifications",async(PersonMessageService service,CancellationToken token,int page=1)=>Results.Ok(await service.InboxAsync(page,token))).RequireAuthorization();
        app.MapPut("/api/v1/notifications/{id:guid}/read",async(Guid id,PersonMessageService service,CancellationToken token)=>await service.ReadAsync(id,token)?Results.NoContent():Results.NotFound()).RequireAuthorization();
        root.MapGet("/people/account-choices",async(Guid propertyId,PeopleService service,CancellationToken token,int page=1,int pageSize=20,string? search=null)=>Found(await service.AccountChoicesAsync(propertyId,new(page,pageSize,search),token)));
        root.MapGet("/people", async (Guid propertyId, PeopleService service, CancellationToken token, int page = 1, int pageSize = 10, string? search = null, string sortBy = "name", string sortDirection = "asc") => Found(await service.ListAsync(propertyId, new(page, pageSize, search, sortBy, sortDirection), token)));
        root.MapGet("/people/{personId:guid}", async (Guid propertyId, Guid personId, PeopleService service, CancellationToken token) => Found(await service.GetAsync(propertyId, personId, token)));
        root.MapPost("/people", async (Guid propertyId, CreatePersonRequest request, PeopleService service, CancellationToken token) => {
            var person = await service.CreateAsync(propertyId, request, token); return person is null ? Results.NotFound() : Results.Created($"/api/v1/properties/{propertyId}/people/{person.Id}", person);
        });
        root.MapPut("/people/{personId:guid}", async (Guid propertyId, Guid personId, UpdatePersonRequest request, PeopleService service, CancellationToken token) => Found(await service.UpdateAsync(propertyId, personId, request, token)));
        root.MapDelete("/people/{personId:guid}", async (Guid propertyId, Guid personId, long version, PeopleService service, CancellationToken token) => await service.DeleteAsync(propertyId, personId, version, token) ? Results.NoContent() : Results.NotFound());
        root.MapGet("/unit-choices", async (Guid propertyId, PeopleService service, CancellationToken token, int page = 1, int pageSize = 20, string? search = null) => Found(await service.UnitChoicesAsync(propertyId, new(page, pageSize, search), token)));
        root.MapGet("/unit-parties", async (Guid propertyId, [Microsoft.AspNetCore.Mvc.FromQuery] Guid[] unitIds, PeopleService service, CancellationToken token) => Found(await service.CurrentUnitPartiesAsync(propertyId, unitIds, token)));
        root.MapGet("/occupancies", async (Guid propertyId, PeopleService service, CancellationToken token, Guid? personId = null, Guid? unitId = null, int page = 1, int pageSize = 10, string? search = null) => Found(await service.OccupanciesAsync(propertyId, personId, unitId, new(page, pageSize, search), token)));
        root.MapGet("/occupancies/{occupancyId:guid}", async (Guid propertyId, Guid occupancyId, PeopleService service, CancellationToken token) => Found(await service.GetOccupancyAsync(propertyId, occupancyId, token)));
        root.MapPost("/occupancies", async (Guid propertyId, CreateOccupancyRequest request, PeopleService service, CancellationToken token) => {
            var link = await service.CreateOccupancyAsync(propertyId, request, token); return link is null ? Results.NotFound() : Results.Created($"/api/v1/properties/{propertyId}/occupancies/{link.Id}", link);
        });
        root.MapPut("/occupancies/{occupancyId:guid}/end", async (Guid propertyId, Guid occupancyId, EndOccupancyRequest request, PeopleService service, CancellationToken token) => Found(await service.EndOccupancyAsync(propertyId, occupancyId, request, token)));
        root.MapGet("/occupancies/{occupancyId:guid}/end-preview", async (Guid propertyId,Guid occupancyId,DateOnly endDate,long version,PeopleService service,CancellationToken token) => Found(await service.EndPreviewAsync(propertyId,occupancyId,endDate,version,token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
