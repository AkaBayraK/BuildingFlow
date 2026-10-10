namespace BuildingFlow.Domain;

// Explicit site access is independent from a user's global Keycloak role.
public sealed class PropertyMembership : BaseEntity
{
    private PropertyMembership() { }
    public PropertyMembership(Guid siteId, string userId)
    {
        if (siteId == Guid.Empty) throw new ArgumentException("Property identity is required.", nameof(siteId));
        PropertyId = siteId;
        UserId = Required(userId, nameof(userId), 200);
    }
    public Guid PropertyId { get; private set; }
    public string UserId { get; private set; } = null!;
}
