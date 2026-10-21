namespace BuildingFlow.Domain;

// Planlama & Bakımlar: one planned piece of work (maintenance, cleaning, inspection, drill) in a management area.
public enum MaintenanceType { MAINTENANCE, CLEANING, INSPECTION, DRILL, OTHER }
public enum MaintenanceStatus { PLANNED, IN_PROGRESS, COMPLETED, CANCELLED }
public sealed class MaintenancePlan : BaseEntity
{
    public const int TitleLength = 200, DescriptionLength = 2000, TextLength = 200;
    private MaintenancePlan() { }
    public MaintenancePlan(Guid propertyId, string title, string? description, MaintenanceType type, DateOnly plannedDate, string? location, string? responsible, MaintenanceStatus status)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property required.");
        PropertyId = propertyId; Edit(title, description, type, plannedDate, location, responsible, status);
    }
    public Guid PropertyId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public MaintenanceType Type { get; private set; }
    public DateOnly PlannedDate { get; private set; }
    public string? Location { get; private set; }
    public string? Responsible { get; private set; }
    public MaintenanceStatus Status { get; private set; }
    public void Edit(string title, string? description, MaintenanceType type, DateOnly plannedDate, string? location, string? responsible, MaintenanceStatus status)
    {
        if (!Enum.IsDefined(type) || !Enum.IsDefined(status)) throw new ArgumentException("Invalid plan type or status.");
        if (plannedDate.Year is < 2000 or > 2100) throw new ArgumentOutOfRangeException(nameof(plannedDate));
        Title = Required(title, nameof(title), TitleLength);
        Description = Optional(description, nameof(description), DescriptionLength);
        Location = Optional(location, nameof(location), TextLength);
        Responsible = Optional(responsible, nameof(responsible), TextLength);
        Type = type; PlannedDate = plannedDate; Status = status;
    }
    private static string? Optional(string? value, string field, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : Required(value, field, maxLength);
}
