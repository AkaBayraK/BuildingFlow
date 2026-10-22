using BuildingFlow.Application;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class PersonDuplicatesTests
{
    [Fact]public void PhoneAndEmailAreComparedNormalizedAndEmptyValuesNeverMatch()
    {
        (string?,string?)[] others=[("+905551234567",null),(null,"Ada@Example.test")];
        Assert.Equal(ErrorCatalog.PersonDuplicatePhone,PersonDuplicates.Contact("0555 123 45 67",null,others));
        Assert.Equal(ErrorCatalog.PersonDuplicateEmail,PersonDuplicates.Contact(null," ada@example.TEST ",others));
        Assert.Null(PersonDuplicates.Contact(null,null,others));Assert.Null(PersonDuplicates.Contact("+905559999999","other@example.test",others));
        Assert.Equal("AB12",PersonDuplicates.IdentityKey(" ab12 "));
    }
}
