namespace BuildingFlow.Domain;

public sealed class MobileRegistration : BaseEntity
{
    private MobileRegistration() { }
    public MobileRegistration(Guid requestId, string fingerprint, string userId, string username, string clientKey)
    {
        if (requestId == Guid.Empty) throw new ArgumentException("Request identity is required.");
        RequestId = requestId; Fingerprint = Required(fingerprint, nameof(fingerprint), 64); UserId = Required(userId, nameof(userId), 200);
        Username = Required(username, nameof(username), 50); ClientKey = Required(clientKey, nameof(clientKey), 64);
    }
    public Guid RequestId { get; private set; }
    public string Fingerprint { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string ClientKey { get; private set; } = null!;
}

public sealed class PersonLinkAttempt : BaseEntity
{
    private PersonLinkAttempt() { }
    public PersonLinkAttempt(string userId, bool succeeded, Guid? personId)
    {
        UserId = Required(userId, nameof(userId), 200); Succeeded = succeeded; PersonId = personId;
    }
    public string UserId { get; private set; } = null!;
    public bool Succeeded { get; private set; }
    public Guid? PersonId { get; private set; }
}
