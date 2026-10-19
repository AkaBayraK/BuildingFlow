using System.Text.Json.Serialization;
namespace BuildingFlow.Application;

public sealed record PersonTransitionPreview(Guid TenantId,Guid PropertyId,string SourceHash,int People,int Ownerships,int Tenancies,int Residencies,int UnprofiledPeople,int ExistingPersonMaps,int ExistingOccupancyMaps,bool CanApply);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ApplyPersonTransitionRequest(string ExpectedSourceHash);
public sealed record PersonTransitionComparison(int People,int PersonMaps,int Occupancies,int OccupancyMaps,int DriftedPeople,int DriftedOccupancies,bool Matches);
public sealed record LegacyLocalProfile(string FirstName,string LastName,string? PhoneNumber,string? Email,string? Address);
public sealed record CompatiblePerson(Guid LegacyId,long LegacyViewVersion,Guid CanonicalId,LegacyLocalProfile LocalProfile,CentralPersonProfile Identity,bool IsEnabled);
public interface IPersonTransitionStore
{
    Task<PersonTransitionPreview?> PreviewAsync(Guid tenant,Guid property,CancellationToken token);
    Task<PersonTransitionComparison?> ApplyAsync(Guid tenant,Guid property,string expectedHash,CancellationToken token);
    Task<PersonTransitionComparison?> CompareAsync(Guid tenant,Guid property,CancellationToken token);
    Task<CompatiblePerson?> ReadAsync(Guid tenant,Guid property,Guid legacyPerson,CancellationToken token);
}
