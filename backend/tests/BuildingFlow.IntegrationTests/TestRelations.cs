using BuildingFlow.Domain;
namespace BuildingFlow.IntegrationTests;
// Fixture rows stand for pre-verification data; PENDING rows deliberately grant nothing.
internal static class TestRelations
{
    public static T Effective<T>(T relation) where T:VerifiableRecord{relation.MarkLegacy();return relation;}
    // A distinct checksum-valid T.C. number per fixture person; one identity number may exist only once per scope.
    public static string Tc(int n)
    {
        var d=(100000000+n%800000000).ToString(System.Globalization.CultureInfo.InvariantCulture).Select(c=>c-'0').ToList();
        d.Add((((d[0]+d[2]+d[4]+d[6]+d[8])*7-(d[1]+d[3]+d[5]+d[7]))%10+10)%10);d.Add(d.Sum()%10);return string.Concat(d);
    }
}
