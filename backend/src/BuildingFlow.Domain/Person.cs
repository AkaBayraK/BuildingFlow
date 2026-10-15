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
    public string? PersonKind { get; private set; }
    public string? Citizenship { get; private set; }
    public string? CountryCode { get; private set; }
    public string? IdentityNumber { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? Gender { get; private set; }
    public string? Address { get; private set; }
    public string? LinkedUserId { get; private set; }
    public void SetProfile(string kind, string citizenship, string country, string identity, DateOnly? birth, string? gender, string? address, string? linkedUserId)
    {
        kind = Required(kind, nameof(kind), 10); citizenship = Required(citizenship, nameof(citizenship), 10);
        country = Required(country, nameof(country), 2).ToUpperInvariant(); identity = Required(identity, nameof(identity), 50);
        if (kind is not ("NATURAL" or "LEGAL") || citizenship is not ("DOMESTIC" or "FOREIGN") || country.Length != 2 || country.Any(c => c is < 'A' or > 'Z') ||
            (citizenship == "DOMESTIC" ? country != "TR" : country == "TR")) throw new ArgumentException("Invalid person profile.");
        if (citizenship == "DOMESTIC" && (identity.Length != (kind == "NATURAL" ? 11 : 10) || identity.Any(c => c is < '0' or > '9') || (kind == "NATURAL" && identity[0] == '0'))) throw new ArgumentException("Invalid identity format.");
        if (citizenship == "FOREIGN" && kind == "NATURAL" && (identity.Length is < 6 or > 30 || identity.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))) throw new ArgumentException("Invalid passport format.");
        if (gender is not (null or "FEMALE" or "MALE" or "OTHER" or "UNSPECIFIED") || (kind == "LEGAL" && (birth != null || gender != null))) throw new ArgumentException("Invalid personal details.");
        address = string.IsNullOrWhiteSpace(address) ? null : Required(address, nameof(address), 500);
        linkedUserId = string.IsNullOrWhiteSpace(linkedUserId) ? null : Required(linkedUserId, nameof(linkedUserId), 200);
        PersonKind=kind; Citizenship=citizenship; CountryCode=country; IdentityNumber=identity; BirthDate=birth; Gender=gender; Address=address; LinkedUserId=linkedUserId;
    }
    public void Update(string firstName, string lastName, string? phoneNumber, string? email, bool enabled)
    {
        // Validate everything before changing tracked domain state.
        firstName = Required(firstName, nameof(firstName), 100);
        lastName = string.IsNullOrWhiteSpace(lastName) && PersonKind == "LEGAL" ? "" : Required(lastName, nameof(lastName), 100);
        phoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : Required(phoneNumber, nameof(phoneNumber), 30);
        email = string.IsNullOrWhiteSpace(email) ? null : Required(email, nameof(email), 254);
        FirstName = firstName; LastName = lastName; PhoneNumber = phoneNumber; Email = email;
        SetEnabled(enabled);
    }
}
