namespace BuildingFlow.Domain;
public sealed class StoredDocument : BaseEntity
{
    public const int MaxBytes = 10 * 1024 * 1024;
    private StoredDocument() { }
    public StoredDocument(Guid property, Guid request, string fingerprint, Guid? announcement, ContentAudience audience,
        string filename, string contentType, long size, string sha256, bool supplierOnly = false)
    {
        if (property == Guid.Empty || request == Guid.Empty || announcement == Guid.Empty || !Enum.IsDefined(audience)) throw new ArgumentException("Invalid document scope.");
        FinanceIdentity.Validate(fingerprint); FinanceIdentity.Validate(sha256);
        filename = Required(filename, nameof(filename), 200);
        if (filename.Any(char.IsControl) || filename.Contains('/') || filename.Contains('\\') || filename is "." or "..") throw new ArgumentException("Invalid filename.");
        if (size is < 1 or > MaxBytes || contentType is not ("application/pdf" or "image/png" or "image/jpeg")) throw new ArgumentException("Invalid file.");
        if (supplierOnly && (audience != ContentAudience.MANAGEMENT || announcement.HasValue)) throw new ArgumentException("Supplier documents are private management records.");
        IsSupplierOnly = supplierOnly;
        PropertyId = property; RequestId = request; RequestFingerprint = fingerprint; AnnouncementId = announcement;
        Audience = audience; FileName = filename; ContentType = contentType; Size = size; Sha256 = sha256;
        ObjectKey = $"properties/{property:N}/documents/{Id:N}";
    }
    public bool IsSupplierOnly { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestFingerprint { get; private set; } = null!;
    public Guid? AnnouncementId { get; private set; }
    public ContentAudience Audience { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long Size { get; private set; }
    public string Sha256 { get; private set; } = null!;
    public string ObjectKey { get; private set; } = null!;
}
