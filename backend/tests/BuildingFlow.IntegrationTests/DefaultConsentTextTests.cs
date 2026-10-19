using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;

// The migration seeds original draft v1 texts with literal hashes; they must stay valid for the parser and the evidence chain.
public sealed class DefaultConsentTextTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    [PostgresFact]
    public async Task SystemTypesShipWithValidDraftTextsThatCanBeApproved()
    {
        await factory.WithDb(async db =>
        {
            var texts = await db.ConsentTexts.AsNoTracking().ToListAsync();
            Assert.Equal(3, texts.Count);
            var types = await db.ConsentTypes.AsNoTracking().Where(t => t.IsSystem).ToListAsync();
            Assert.All(types, type => Assert.Single(texts, x => x.ConsentTypeId == type.Id));
            foreach (var text in texts)
            {
                Assert.Equal(1, text.TextVersion); Assert.True(text.RequiresReconsent);
                Assert.Null(ConsentMarkup.Parse(text.Body).Error);
                Assert.Equal(text.Body, ConsentMarkup.Normalize(text.Body).Trim('\n'));
                Assert.Equal(ConsentEvidence.Sha256(text.Body), text.BodySha256);
                Assert.Equal(ConsentEvidence.TextFingerprint(text.Title, text.Body, true), text.Fingerprint);
                Assert.InRange(text.Title.Length, 1, ConsentText.MaxTitleLength);
                Assert.Equal("system", text.CreatedBy);
            }
        });
        // A new account can approve the shipped text straight away; the centre's next publish becomes v2.
        var user = Guid.NewGuid().ToString();
        var property = new Property("Default text fixture", "City", "District", "Address");
        await factory.WithDb(async db => { db.AddRange(property, new PropertyMembership(property.Id, user)); await db.SaveChangesAsync(); });
        using var client = TestToken.Client(factory, user, "MANAGER");
        foreach (var code in new[] { "KVKK_INFORMATION", "PROFILE_PHOTO", "REQUEST_PHOTO" })
        {
            var item = await ConsentTestHelper.ItemAsync(client, code);
            Assert.Equal(1, item.CurrentText!.TextVersion);
            var result = (await (await ConsentTestHelper.CommandAsync(client, ConsentTestHelper.Approve(code, item.CurrentText))).Content.ReadFromJsonAsync<MobileConsentResult>())!;
            Assert.Equal(ConsentStates.Active, result.Status.State);
        }
        Assert.Equal(2, (await ConsentTestHelper.PublishAsync(factory, "PROFILE_PHOTO", "Yeni sürüm.")).TextVersion);
    }
}
