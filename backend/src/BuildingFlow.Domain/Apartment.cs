namespace BuildingFlow.Domain;

public sealed class Apartment : BaseEntity
{
    private Apartment() { }

    internal Apartment(Guid blockId, string number, int floor, decimal grossSquareMeters)
    {
        if (blockId == Guid.Empty)
        {
            throw new ArgumentException("Block ID is required.", nameof(blockId));
        }

        if (grossSquareMeters <= 0 || grossSquareMeters > 99999999.99m)
        {
            throw new ArgumentOutOfRangeException(nameof(grossSquareMeters));
        }

        BlockId = blockId;
        Number = Required(number, nameof(number), 30);
        Floor = floor;
        GrossSquareMeters = grossSquareMeters;
    }

    public Guid BlockId { get; private set; }
    public string Number { get; private set; } = null!;
    public int Floor { get; private set; }
    public decimal GrossSquareMeters { get; private set; }
}
