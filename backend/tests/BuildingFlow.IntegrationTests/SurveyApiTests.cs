using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Anketler API against a dedicated PostgreSQL test database: manager lifecycle, resident answers, anonymity and scope.
public sealed class SurveyApiTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today=>DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private static string Root(Guid p)=>$"/api/v1/properties/{p}/surveys";
    private sealed record Resident(string User,Guid Unit,string Role);
    private sealed record Fixture(Property P,string Manager,Resident[] Residents);
    // Units 1 and 2 have an owner and a tenant each; unit 3 only an owner without an app account.
    private async Task<Fixture> Seed()
    {
        var manager=Guid.NewGuid().ToString();var p=new Property("Anket "+manager,"City","District","Address",PropertyType.APARTMENT);var residents=new List<Resident>();
        await factory.WithDb(async db=>{
            db.Add(p);db.Add(new PropertyMembership(p.Id,manager));
            for(var i=1;i<=3;i++){
                var u=p.AddUnit(i.ToString(),i,90m);db.Add(u);
                foreach(var type in i==3?new[]{OccupancyType.OWNER}:new[]{OccupancyType.OWNER,OccupancyType.TENANT}){
                    var user=i==3?null:Guid.NewGuid().ToString();var person=new Person(p.Id,"Sakin",$"{i}{type}","5550000000","fixture@example.test");person.SetProfile("NATURAL","FOREIGN","US","FX"+Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),null,null,null,user);
                    if(user is not null){db.Add(new PropertyMembership(p.Id,user));residents.Add(new(user,u.Id,type.ToString()));}
                    db.Add(person);db.Add(new UnitOccupancy(p.Id,u.Id,person.Id,type,Today.AddYears(-1)));
                }
            }
            await db.SaveChangesAsync();});
        return new(p,manager,residents.ToArray());
    }
    private static SurveyRequest Request(string title="Ortak Alan Temizlik Memnuniyeti",string audience="ALL_RESIDENTS",string mode="PER_UNIT",bool anonymous=false,long version=0,DateOnly? start=null)=>new(title," Temizlik hizmetleri ","SERVICE",start??Today,(start??Today).AddDays(14),audience,mode,anonymous,[
        new(" Genel memnuniyet ","RATING",true,null),
        new("En çok hangi alan?","SINGLE_CHOICE",true,[" Merdiven ","Otopark","Bahçe"]),
        new("İyileştirme önerileri","MULTIPLE_CHOICE",false,["Sıklık","Malzeme","Personel"]),
        new("Görüşünüz","TEXT",false,null)],version);
    private static async Task<SurveyDetail> Create(HttpClient c,Guid p,SurveyRequest r){var res=await c.PostAsJsonAsync(Root(p),r);Assert.Equal(HttpStatusCode.Created,res.StatusCode);return(await res.Content.ReadFromJsonAsync<SurveyDetail>())!;}
    private static async Task<SurveyDetail> Put(HttpClient c,string path,object body){var res=await c.PutAsJsonAsync(path,body);Assert.Equal(HttpStatusCode.OK,res.StatusCode);return(await res.Content.ReadFromJsonAsync<SurveyDetail>())!;}
    private static ResidentSurveySubmit Answer(Guid p,Resident r,SurveyDetail s,int rating=5,int choice=0,string? text=null)=>new(p,r.Unit,r.Role,[
        new(s.Questions[0].Id,null,rating,null),new(s.Questions[1].Id,[s.Questions[1].Options[choice].Id],null,null),
        new(s.Questions[2].Id,[s.Questions[2].Options[0].Id,s.Questions[2].Options[2].Id],null,null),new(s.Questions[3].Id,null,null,text)]);
    private HttpClient As(Resident r)=>TestToken.Client(factory,r.User,"RESIDENT");
    private static Task<HttpResponseMessage> Submit(HttpClient c,Guid survey,ResidentSurveySubmit body)=>c.PostAsJsonAsync($"/api/v1/mobile/surveys/{survey}/responses",body);

    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task ManagersDraftEditPublishExtendCopyAndDeleteWithVersions(string role)
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Manager,role);
        var draft=await Create(c,x.P.Id,Request(" Temizlik "));
        Assert.Equal("Temizlik",draft.Survey.Title);Assert.Equal("Temizlik hizmetleri",draft.Survey.Description);Assert.Equal("DRAFT",draft.Survey.State);Assert.Equal(4,draft.Survey.QuestionCount);
        Assert.Equal(new[]{"Merdiven","Otopark","Bahçe"},draft.Questions[1].Options.Select(o=>o.Text));Assert.Equal(1,draft.Survey.Version);
        var edited=await Put(c,Root(x.P.Id)+"/"+draft.Survey.Id,Request("Temizlik 2",version:1) with{Questions=[new("Tek soru","TEXT",true,null)]});
        Assert.Equal(1,edited.Survey.QuestionCount);Assert.Equal(2,edited.Survey.Version);
        await factory.WithDb(async db=>Assert.Equal(4,await db.SurveyQuestions.IgnoreQueryFilters().CountAsync(q=>q.SurveyId==draft.Survey.Id&&q.IsDeleted)));
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(Root(x.P.Id)+"/"+draft.Survey.Id,Request(version:1))).StatusCode);
        var published=await Put(c,Root(x.P.Id)+$"/{draft.Survey.Id}/publish",new SurveyVersionRequest(2));Assert.Equal("ACTIVE",published.Survey.State);Assert.NotNull(published.Survey.PublishedAt);
        var locked=await c.PutAsJsonAsync(Root(x.P.Id)+"/"+draft.Survey.Id,Request(version:published.Survey.Version));Assert.Equal(HttpStatusCode.Conflict,locked.StatusCode);Assert.Contains("BF_SURVEY_STATE",await locked.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict,(await c.DeleteAsync(Root(x.P.Id)+$"/{draft.Survey.Id}?version={published.Survey.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(Root(x.P.Id)+$"/{draft.Survey.Id}/schedule",new SurveyScheduleRequest(Today.AddDays(-1),published.Survey.Version))).StatusCode);
        var extended=await Put(c,Root(x.P.Id)+$"/{draft.Survey.Id}/schedule",new SurveyScheduleRequest(Today.AddDays(30),published.Survey.Version));Assert.Equal(Today.AddDays(30),extended.Survey.EndDate);
        var copy=await c.PostAsync(Root(x.P.Id)+$"/{draft.Survey.Id}/copy",null);Assert.Equal(HttpStatusCode.Created,copy.StatusCode);var copied=(await copy.Content.ReadFromJsonAsync<SurveyDetail>())!;
        Assert.Equal("DRAFT",copied.Survey.State);Assert.Equal("Tek soru",Assert.Single(copied.Questions).Text);Assert.NotEqual(draft.Questions[0].Id,copied.Questions[0].Id);
        Assert.Equal(HttpStatusCode.NoContent,(await c.DeleteAsync(Root(x.P.Id)+$"/{copied.Survey.Id}?version={copied.Survey.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(x.P.Id)+"/"+copied.Survey.Id)).StatusCode);
        var empty=await Create(c,x.P.Id,Request("Boş") with{Questions=[]});var refused=await c.PutAsJsonAsync(Root(x.P.Id)+$"/{empty.Survey.Id}/publish",new SurveyVersionRequest(1));Assert.Equal(HttpStatusCode.Conflict,refused.StatusCode);
        await factory.WithDb(async db=>{var row=await db.Surveys.IgnoreQueryFilters().SingleAsync(s=>s.Id==copied.Survey.Id);Assert.True(row.IsDeleted);Assert.Equal(x.Manager,row.CreatedBy);
            Assert.True(await db.SurveyOptions.IgnoreQueryFilters().Where(o=>o.SurveyId==copied.Survey.Id).AllAsync(o=>o.IsDeleted));});
    }
    [PostgresFact] public async Task InvalidSurveysAreCodedAndNothingIsSaved()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Manager,"MANAGER");
        async Task Bad(SurveyRequest r,string code){var res=await c.PostAsJsonAsync(Root(x.P.Id),r);Assert.Equal(HttpStatusCode.BadRequest,res.StatusCode);Assert.Contains(code,await res.Content.ReadAsStringAsync());}
        await Bad(Request(" "),"BF_SURVEY_INVALID");await Bad(Request() with{Category="FIRE"},"BF_SURVEY_INVALID");await Bad(Request() with{Category="service"},"BF_SURVEY_INVALID");
        await Bad(Request() with{EndDate=Today.AddDays(-1)},"BF_SURVEY_INVALID");await Bad(Request() with{Audience="ALL"},"BF_SURVEY_INVALID");await Bad(Request() with{ResponseMode="ONE"},"BF_SURVEY_INVALID");
        await Bad(Request() with{Description=new string('x',2001)},"BF_SURVEY_INVALID");await Bad(Request(version:3),"BF_SURVEY_INVALID");
        await Bad(Request() with{Questions=[new("Q","SINGLE_CHOICE",true,["Tek"])]},"BF_SURVEY_QUESTION_INVALID");
        await Bad(Request() with{Questions=[new("Q","SINGLE_CHOICE",true,["Evet","evet"])]},"BF_SURVEY_QUESTION_INVALID");
        await Bad(Request() with{Questions=[new("Q","RATING",true,["1","2"])]},"BF_SURVEY_QUESTION_INVALID");
        await Bad(Request() with{Questions=[new("Q","DATE",true,null)]},"BF_SURVEY_QUESTION_INVALID");
        await Bad(Request() with{Questions=[new(new string('q',501),"TEXT",true,null)]},"BF_SURVEY_QUESTION_INVALID");
        await Bad(Request() with{Questions=Enumerable.Range(0,31).Select(i=>new SurveyQuestionRequest($"Q{i}","TEXT",false,null)).ToArray()},"BF_SURVEY_QUESTION_INVALID");
        await factory.WithDb(async db=>Assert.False(await db.Surveys.AnyAsync(s=>s.PropertyId==x.P.Id)));
        foreach(var bad in new[]{"?state=OPEN","?category=FIRE","?page=0","?pageSize=101",$"?from={Today:yyyy-MM-dd}&to={Today.AddDays(-1):yyyy-MM-dd}"})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Root(x.P.Id)+bad)).StatusCode);
        var valid=await Create(c,x.P.Id,Request());
        await factory.WithDb(async db=>{
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE surveys SET category='FIRE' WHERE id={valid.Survey.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE surveys SET status='CLOSED' WHERE id={valid.Survey.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE survey_questions SET type='DATE' WHERE survey_id={valid.Survey.Id}"));});
    }
    [PostgresFact] public async Task ResidentsAnswerOncePerUnitAndResultsAggregate()
    {
        var x=await Seed();using var m=TestToken.Client(factory,x.Manager,"MANAGER");
        var s=await Create(m,x.P.Id,Request());s=await Put(m,Root(x.P.Id)+$"/{s.Survey.Id}/publish",new SurveyVersionRequest(1));
        Assert.Equal(3,s.Survey.EligibleCount);
        var (owner1,tenant1,owner2)=(x.Residents[0],x.Residents[1],x.Residents[2]);
        using(var c=As(owner1)){
            var open=(await c.GetFromJsonAsync<ResidentSurveyItem[]>($"/api/v1/mobile/surveys?propertyId={x.P.Id}&unitId={owner1.Unit}&role={owner1.Role}"))!;Assert.False(Assert.Single(open).Answered);
            var detail=(await c.GetFromJsonAsync<ResidentSurveyDetail>($"/api/v1/mobile/surveys/{s.Survey.Id}?propertyId={x.P.Id}&unitId={owner1.Unit}&role={owner1.Role}"))!;Assert.Equal(4,detail.Questions.Count);
            // Invalid answers: missing required, foreign option, rating out of range, unknown question, text on a choice question.
            var good=Answer(x.P.Id,owner1,s,text:" Daha sık temizlik ");
            foreach(var bad in new[]{good with{Answers=good.Answers!.Skip(1).ToArray()},good with{Answers=[good.Answers![0],new(s.Questions[1].Id,[s.Questions[2].Options[0].Id],null,null)]},
                good with{Answers=[new(s.Questions[0].Id,null,6,null),good.Answers![1]]},good with{Answers=[..good.Answers!,new(Guid.NewGuid(),null,1,null)]},
                good with{Answers=[good.Answers![0],new(s.Questions[1].Id,[s.Questions[1].Options[0].Id,s.Questions[1].Options[1].Id],null,null)]}}){
                var r=await Submit(c,s.Survey.Id,bad);Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);Assert.Contains("BF_SURVEY_ANSWER_INVALID",await r.Content.ReadAsStringAsync());}
            await factory.WithDb(async db=>Assert.False(await db.SurveyParticipations.AnyAsync(p=>p.SurveyId==s.Survey.Id)));
            var ok=await Submit(c,s.Survey.Id,good);Assert.Equal(HttpStatusCode.Created,ok.StatusCode);
            var again=await Submit(c,s.Survey.Id,good);Assert.Equal(HttpStatusCode.Conflict,again.StatusCode);Assert.Contains("BF_SURVEY_ALREADY_ANSWERED",await again.Content.ReadAsStringAsync());
            Assert.True(Assert.Single((await c.GetFromJsonAsync<ResidentSurveyItem[]>($"/api/v1/mobile/surveys?propertyId={x.P.Id}&unitId={owner1.Unit}&role={owner1.Role}"))!).Answered);
        }
        using(var c=As(tenant1))Assert.Equal(HttpStatusCode.Conflict,(await Submit(c,s.Survey.Id,Answer(x.P.Id,tenant1,s))).StatusCode);
        using(var c=As(owner2))Assert.Equal(HttpStatusCode.Created,(await Submit(c,s.Survey.Id,Answer(x.P.Id,owner2,s,rating:3,choice:1))).StatusCode);
        var results=(await m.GetFromJsonAsync<SurveyResults>(Root(x.P.Id)+$"/{s.Survey.Id}/results"))!;Assert.False(results.Hidden);Assert.Equal(2,results.Survey.ResponseCount);
        Assert.Equal(4m,results.Questions[0].RatingAverage);Assert.Equal(new[]{0,0,1,0,1},results.Questions[0].RatingCounts);
        Assert.Equal(new[]{1,1,0},results.Questions[1].Options.Select(o=>o.Count));Assert.Equal(new[]{2,0,2},results.Questions[2].Options.Select(o=>o.Count));
        var text=Assert.Single(results.Questions[3].Texts);Assert.Equal("Daha sık temizlik",text.Text);Assert.Equal("1",text.UnitNumber);Assert.Equal(1,results.Questions[3].Answered);
        var closed=await Put(m,Root(x.P.Id)+$"/{s.Survey.Id}/close",new SurveyVersionRequest(s.Survey.Version+2));Assert.Equal("COMPLETED",closed.Survey.State);Assert.Equal(3,closed.Survey.EligibleCount);
        using(var c=As(x.Residents[3])){var late=await Submit(c,s.Survey.Id,Answer(x.P.Id,x.Residents[3],s));Assert.Equal(HttpStatusCode.Conflict,late.StatusCode);Assert.Contains("BF_SURVEY_NOT_OPEN",await late.Content.ReadAsStringAsync());}
        var overview=(await m.GetFromJsonAsync<SurveyOverview>(Root(x.P.Id)+"/overview"))!;Assert.Equal(1,overview.Completed);Assert.Equal(2,overview.CompletedResponses);Assert.Equal(3,overview.CompletedEligible);
        Assert.Equal(s.Survey.Id,overview.LastCompleted!.Id);Assert.Equal(s.Survey.Id,Assert.Single(overview.TopParticipation).Id);Assert.Equal(1,overview.Categories.Single(c=>c.Key=="SERVICE").Count);
    }
    [PostgresFact] public async Task AnonymousSurveyKeepsNoIdentityAndHidesSmallResults()
    {
        var x=await Seed();using var m=TestToken.Client(factory,x.Manager,"MANAGER");
        var s=await Create(m,x.P.Id,Request(mode:"PER_ACCOUNT",anonymous:true));s=await Put(m,Root(x.P.Id)+$"/{s.Survey.Id}/publish",new SurveyVersionRequest(1));Assert.Equal(4,s.Survey.EligibleCount);
        foreach(var r in x.Residents.Take(2)){using var c=As(r);Assert.Equal(HttpStatusCode.Created,(await Submit(c,s.Survey.Id,Answer(x.P.Id,r,s,text:"Gizli görüş "+r.Role))).StatusCode);}
        var hidden=(await m.GetFromJsonAsync<SurveyResults>(Root(x.P.Id)+$"/{s.Survey.Id}/results"))!;Assert.True(hidden.Hidden);Assert.Empty(hidden.Questions);Assert.Equal(2,hidden.Survey.ResponseCount);
        using(var c=As(x.Residents[2]))Assert.Equal(HttpStatusCode.Created,(await Submit(c,s.Survey.Id,Answer(x.P.Id,x.Residents[2],s,text:"Üçüncü"))).StatusCode);
        var raw=await m.GetStringAsync(Root(x.P.Id)+$"/{s.Survey.Id}/results");foreach(var r in x.Residents)Assert.DoesNotContain(r.User,raw);
        var shown=(await m.GetFromJsonAsync<SurveyResults>(Root(x.P.Id)+$"/{s.Survey.Id}/results"))!;Assert.False(shown.Hidden);Assert.All(shown.Questions[3].Texts,t=>Assert.Null(t.UnitNumber));Assert.Equal(3,shown.Questions[3].Texts.Count);
        await factory.WithDb(async db=>{
            var responses=await db.SurveyResponses.Where(r=>r.SurveyId==s.Survey.Id).ToListAsync();Assert.Equal(3,responses.Count);
            Assert.All(responses,r=>{Assert.Null(r.ParticipationId);Assert.Null(r.UnitId);Assert.Equal("anonymous",r.CreatedBy);Assert.Equal(TimeSpan.Zero,r.CreatedAt.UtcDateTime.TimeOfDay);});
            Assert.True(await db.SurveyAnswers.Where(a=>a.SurveyId==s.Survey.Id).AllAsync(a=>a.CreatedBy=="anonymous"&&a.ModifiedBy=="anonymous"));
            Assert.Equal(3,await db.SurveyParticipations.CountAsync(p=>p.SurveyId==s.Survey.Id));
            var participation=await db.SurveyParticipations.FirstAsync(p=>p.SurveyId==s.Survey.Id);
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE survey_responses SET participation_id={participation.Id} WHERE survey_id={s.Survey.Id}"));});
    }
    [PostgresFact] public async Task ScopeRolesAudienceAndConcurrentBallots()
    {
        var x=await Seed();var other=await Seed();using var m=TestToken.Client(factory,x.Manager,"MANAGER");
        var owners=await Create(m,x.P.Id,Request("Malik anketi",audience:"OWNERS"));owners=await Put(m,Root(x.P.Id)+$"/{owners.Survey.Id}/publish",new SurveyVersionRequest(1));Assert.Equal(3,owners.Survey.EligibleCount);
        var future=await Create(m,x.P.Id,Request("Gelecek",start:Today.AddDays(5)));future=await Put(m,Root(x.P.Id)+$"/{future.Survey.Id}/publish",new SurveyVersionRequest(1));Assert.Equal("SCHEDULED",future.Survey.State);
        await Create(m,x.P.Id,Request("Taslak"));
        using var anon=factory.CreateClient();using var outsider=TestToken.Client(factory,other.Manager,"MANAGER");using var accountant=TestToken.Client(factory,x.Manager,"ACCOUNTANT");using var resident=As(x.Residents[0]);
        foreach(var suffix in new[]{"","/overview",$"/{owners.Survey.Id}",$"/{owners.Survey.Id}/results"}){
            Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(Root(x.P.Id)+suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(Root(x.P.Id)+suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await accountant.GetAsync(Root(x.P.Id)+suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await resident.GetAsync(Root(x.P.Id)+suffix)).StatusCode);}
        var page=(await m.GetFromJsonAsync<SurveyPage>(Root(x.P.Id)))!;Assert.Equal(3,page.TotalCount);
        Assert.Equal(new[]{"DRAFT:1","SCHEDULED:1","ACTIVE:1","EXPIRED:0","COMPLETED:0"},page.StateCounts.Select(c=>$"{c.Key}:{c.Count}"));
        Assert.Equal("Malik anketi",Assert.Single((await m.GetFromJsonAsync<SurveyPage>(Root(x.P.Id)+"?state=ACTIVE&search=malik"))!.Items).Title);
        Assert.Equal(3,(await m.GetFromJsonAsync<SurveyPage>(Root(x.P.Id)+"?category=SERVICE"))!.CategoryCounts.Single(c=>c.Key=="SERVICE").Count);
        var tenant=x.Residents[1];using(var c=As(tenant)){
            Assert.Empty((await c.GetFromJsonAsync<ResidentSurveyItem[]>($"/api/v1/mobile/surveys?propertyId={x.P.Id}&unitId={tenant.Unit}&role={tenant.Role}"))!);
            Assert.Equal(HttpStatusCode.Forbidden,(await Submit(c,owners.Survey.Id,Answer(x.P.Id,tenant,owners))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await Submit(c,owners.Survey.Id,Answer(x.P.Id,tenant,owners) with{Role="OWNER"})).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict,(await Submit(c,future.Survey.Id,Answer(x.P.Id,tenant,future))).StatusCode);}
        var owner=x.Residents[0];
        Assert.Equal(HttpStatusCode.Forbidden,(await resident.GetAsync($"/api/v1/mobile/surveys?propertyId={x.P.Id}&unitId={x.Residents[2].Unit}&role=OWNER")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await resident.GetAsync($"/api/v1/mobile/surveys?propertyId={other.P.Id}&unitId={owner.Unit}&role=OWNER")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await resident.GetAsync($"/api/v1/mobile/surveys?propertyId={x.P.Id}&unitId={owner.Unit}&role=MANAGER")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync($"/api/v1/mobile/surveys?propertyId={x.P.Id}&unitId={owner.Unit}&role=OWNER")).StatusCode);
        using var second=As(owner);
        var both=await Task.WhenAll(Submit(resident,owners.Survey.Id,Answer(x.P.Id,owner,owners)),Submit(second,owners.Survey.Id,Answer(x.P.Id,owner,owners)));
        Assert.Equal(new[]{HttpStatusCode.Conflict,HttpStatusCode.Created},both.Select(r=>r.StatusCode).OrderBy(s=>s.ToString()));
        await factory.WithDb(async db=>{Assert.Equal(1,await db.SurveyParticipations.CountAsync(p=>p.SurveyId==owners.Survey.Id));Assert.Equal(1,(await db.Surveys.SingleAsync(s=>s.Id==owners.Survey.Id)).ResponseCount);});
        Assert.Equal(HttpStatusCode.NotFound,(await m.GetAsync(Root(other.P.Id)+"/"+owners.Survey.Id)).StatusCode);
        await factory.WithDb(async db=>{(await db.Properties.SingleAsync(p=>p.Id==x.P.Id)).SetEnabled(false);await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Conflict,(await m.PostAsJsonAsync(Root(x.P.Id),Request())).StatusCode);Assert.Equal(HttpStatusCode.OK,(await m.GetAsync(Root(x.P.Id))).StatusCode);
    }
}
