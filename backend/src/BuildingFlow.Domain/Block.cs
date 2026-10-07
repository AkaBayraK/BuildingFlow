namespace BuildingFlow.Domain;

public sealed class Block : BaseEntity
{
    private readonly List<Apartment> _apartments = [];

    private Block() { }

    internal Block(Guid siteId, string name)
    {
        if (siteId == Guid.Empty)
        {
            throw new ArgumentException("Site ID is required.", nameof(siteId));
        }

        SiteId = siteId;
        Name = Required(name, nameof(name), 100);
    }

    public Guid SiteId { get; private set; }
    public string Name { get; private set; } = null!;
    public IReadOnlyCollection<Apartment> Apartments => _apartments.AsReadOnly();

    // Load the complete active collection before adding to a persisted aggregate.
    public Apartment AddApartment(string number, int floor, decimal grossSquareMeters)
    {
        number = Required(number, nameof(number), 30);
        if (_apartments.Any(apartment => !apartment.IsDeleted && string.Equals(apartment.Number, number, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Apartment number already exists in this block.", nameof(number));
        }

        var apartment = new Apartment(Id, number, floor, grossSquareMeters);
        _apartments.Add(apartment);
        return apartment;
    }
}
