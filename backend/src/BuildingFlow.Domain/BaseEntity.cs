namespace BuildingFlow.Domain;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public long Version { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public string CreatedBy { get; protected set; } = null!;
    public DateTimeOffset ModifiedAt { get; protected set; }
    public string ModifiedBy { get; protected set; } = null!;
    public bool IsDeleted { get; protected set; }
    public bool IsEnabled { get; protected set; } = true;
    public void SetEnabled(bool enabled) => IsEnabled = enabled;
    protected static string Required(string value, string field, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, field);
        value = value.Trim();
        if (value.Length > maxLength) throw new ArgumentException($"{field} exceeds {maxLength} characters.", field);
        return value;
    }
}
