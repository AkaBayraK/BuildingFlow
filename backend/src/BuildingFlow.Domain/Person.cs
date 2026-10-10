namespace BuildingFlow.Domain;

public sealed class Person : BaseEntity
{
    private Person() { }
    public Person(Guid propertyId, string firstName, string lastName, string? phoneNumber = null, string? email = null)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property identity is required.");
        PropertyId = propertyId;
        Update(firstName, lastName, phoneNumber, email, true);
    }
    public Guid PropertyId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public void Update(string firstName, string lastName, string? phoneNumber, string? email, bool enabled)
    {
        // Validate everything before changing tracked domain state.
        firstName = Required(firstName, nameof(firstName), 100);
        lastName = Required(lastName, nameof(lastName), 100);
        phoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : Required(phoneNumber, nameof(phoneNumber), 30);
        email = string.IsNullOrWhiteSpace(email) ? null : Required(email, nameof(email), 254);
        FirstName = firstName; LastName = lastName; PhoneNumber = phoneNumber; Email = email;
        SetEnabled(enabled);
    }
}
