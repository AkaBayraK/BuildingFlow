namespace BuildingFlow.Domain;

public sealed class Site : BaseEntity
{
    private readonly List<Block> _blocks = [];

    private Site() { }

    public Site(string name, string city, string district, string address)
    {
        Name = Required(name, nameof(name), 200);
        City = Required(city, nameof(city), 100);
        District = Required(district, nameof(district), 100);
        Address = Required(address, nameof(address), 500);
    }

    public string Name { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string District { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public IReadOnlyCollection<Block> Blocks => _blocks.AsReadOnly();

    // Load the complete active collection before adding to a persisted aggregate.
    public Block AddBlock(string name)
    {
        name = Required(name, nameof(name), 100);
        if (_blocks.Any(block => !block.IsDeleted && string.Equals(block.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Block name already exists in this site.", nameof(name));
        }

        var block = new Block(Id, name);
        _blocks.Add(block);
        return block;
    }
}
