using System.Collections.ObjectModel;
using System.Globalization;
namespace BuildingFlow.Application;

// Public messages are defined here; technical exception details never become API messages.
public sealed record ApiErrorMessage(string Code, string Message, string Severity, IReadOnlyDictionary<string, object> Parameters);
public sealed record ErrorDefinition(string Code, string Message, string Severity, params string[] ParameterNames)
{
    public ApiErrorMessage Create(params (string Key, object Value)[] parameters)
    {
        var values = parameters.ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal);
        if (values.Count != ParameterNames.Length || ParameterNames.Any(key => !values.ContainsKey(key)))
            throw new ArgumentException("Invalid error parameters.");
        foreach (var (key, value) in values)
            if (!(key == "field" && value is string field && ErrorCatalog.FieldLabels.ContainsKey(field)) &&
                !(key == "max" && value is int max && max is >= 1 and <= 1000))
                throw new ArgumentException("Invalid error parameter value.");
        var text = Message;
        foreach (var (key, value) in values)
            text = text.Replace("{" + key + "}", key == "field" ? ErrorCatalog.FieldLabels[(string)value] : Convert.ToString(value, CultureInfo.InvariantCulture), StringComparison.Ordinal);
        return new(Code, text, Severity, new ReadOnlyDictionary<string, object>(values));
    }
}

public static class ErrorCatalog
{
    public static readonly ErrorDefinition MeterInvalid = new("BF_METER_INVALID", "Geçerli sayaç adı, türü ve istek kimliği gerekli.", "error");
    public static readonly ErrorDefinition ReadingInvalid = new("BF_READING_INVALID", "2000 veya sonrası, gelecekte olmayan sabit tarih; sıfır veya pozitif en fazla üç ondalıklı endeks, gerekçe ve geçerli sürüm gerekli.", "error");
    public static readonly ErrorDefinition MeterConflict = new("BF_METER_CONFLICT", "Sayaç adı, okuma tarihi veya istek kimliği çakışıyor. Güncel listeyi kontrol edin.", "warning");
    public static readonly ErrorDefinition MeterInvalidUnit = new("BF_METER_INVALID_UNIT", "Aynı yönetim alanındaki aktif bağımsız bölümü seçin.", "warning");
    public static readonly ErrorDefinition MeterInactive = new("BF_METER_INACTIVE", "Pasif sayaca yeni okuma veya düzeltme eklenemez.", "warning");
    public static readonly ErrorDefinition ReadingOrder = new("BF_READING_ORDER", "Endeks önceki okumadan küçük veya sonraki okumadan büyük olamaz.", "warning");

    public static readonly ErrorDefinition ConstructionYearInvalid = new("BF_CONSTRUCTION_YEAR_INVALID", "Yapım yılı 1800 ile içinde bulunulan yıl arasında olmalı.", "error");
    public static readonly ErrorDefinition HeatingTypeInvalid = new("BF_HEATING_TYPE_INVALID", "Geçerli bir ısıtma tipi seçin.", "error");
    public static readonly ErrorDefinition ParkingTypeInvalid = new("BF_PARKING_TYPE_INVALID", "Geçerli bir otopark seçeneği seçin.", "error");
    public static readonly ErrorDefinition BuildingFeaturesApartmentOnly = new("BF_BUILDING_FEATURES_APARTMENT_ONLY", "Yapım yılı, ısıtma, asansör ve otopark bu türde bina/blok üzerinde tutulur.", "error");
    public static readonly ErrorDefinition NetAreaInvalid = new("BF_NET_AREA_INVALID", "Net alan 0'dan büyük, brüt alandan büyük olmayan ve en fazla iki ondalık basamaklı olmalı.", "error");
    public static readonly ErrorDefinition RoomLayoutInvalid = new("BF_ROOM_LAYOUT_INVALID", "Geçerli bir oda sayısı seçin.", "error");
    public static readonly ErrorDefinition FloorPositionInvalid = new("BF_FLOOR_POSITION_INVALID", "Bulunduğu kat seçimi kat numarasıyla uyumlu olmalı.", "error");
    public static readonly ErrorDefinition FloorExceedsFloorCount = new("BF_FLOOR_EXCEEDS_FLOOR_COUNT", "Bulunduğu kat binanın kat sayısını aşamaz.", "error");
    public static readonly ErrorDefinition BathroomCountInvalid = new("BF_BATHROOM_COUNT_INVALID", "Banyo sayısı 0 ile 6 (6 ve üzeri) arasında olmalı.", "error");
    public static readonly ErrorDefinition KitchenTypeInvalid = new("BF_KITCHEN_TYPE_INVALID", "Açık (Amerikan) veya kapalı mutfak seçin.", "error");

    public static readonly ErrorDefinition ContentInvalid = new("BF_CONTENT_INVALID", "Başlık 1–200, içerik 1–10000 karakter olmalı; geçerli hedef seçin.", "error");
    public static readonly ErrorDefinition ContentState = new("BF_CONTENT_STATE", "Bu işlem duyurunun mevcut yayın durumunda yapılamaz.", "warning");
    public static readonly ErrorDefinition DocumentInvalid = new("BF_DOCUMENT_INVALID", "Geçerli dosya adı, PDF/JPEG/PNG uzantısı ve dosya imzası gerekli.", "error");
    public static readonly ErrorDefinition DocumentTooLarge = new("BF_DOCUMENT_TOO_LARGE", "Dosya en fazla 10 MiB olabilir.", "error");
    public static readonly ErrorDefinition DocumentRequestConflict = new("BF_DOCUMENT_REQUEST_CONFLICT", "Bu yükleme kimliği farklı veya silinmiş bir belge için kullanılmış.", "warning");
    public static readonly ErrorDefinition DocumentLimit = new("BF_DOCUMENT_LIMIT", "Bir duyuruya en fazla 10 dosya eklenebilir.", "warning");
    public static readonly ErrorDefinition StorageUnavailable = new("BF_STORAGE_UNAVAILABLE", "Dosya deposuna erişilemiyor. Lütfen tekrar deneyin.", "error");
    public static readonly ErrorDefinition ContentPropertyHistory = new("BF_CONTENT_PROPERTY_HISTORY", "Duyuru veya belge geçmişi bulunan yönetim alanı silinemez.", "warning");

    public static readonly ErrorDefinition ExpenseCategoryInvalid = new("BF_EXPENSE_CATEGORY_INVALID", "Geçerli gider kategorisi seçin.", "error");
    public static readonly ErrorDefinition ExpenseDateInvalid = new("BF_EXPENSE_DATE_INVALID", "2000 veya sonrası için gerçekleşmiş bir gider tarihi girin; gelecek tarih kullanılamaz.", "error");
    public static readonly ErrorDefinition ExpenseReferenceInvalid = new("BF_EXPENSE_REFERENCE_INVALID", "Belge referansı en fazla 100 karakter olabilir.", "error");
    public static readonly ErrorDefinition BudgetAmountInvalid = new("BF_BUDGET_AMOUNT_INVALID", "Bütçe 0 veya pozitif, en fazla 99999999,99 ve iki ondalık basamak olmalı.", "error");
    public static readonly ErrorDefinition ExpenseRequestConflict = new("BF_EXPENSE_REQUEST_CONFLICT", "Bu istek kimliği farklı bir gider için kullanılmış.", "warning");
    public static readonly ErrorDefinition ExpenseAlreadyCancelled = new("BF_EXPENSE_ALREADY_CANCELLED", "Bu gider zaten iptal edilmiş; geçmişi yeniden yazılamaz.", "warning");
    public static readonly ErrorDefinition BudgetRequestConflict = new("BF_BUDGET_REQUEST_CONFLICT", "Bu istek kimliği farklı bir bütçe revizyonu için kullanılmış.", "warning");

    public static readonly ErrorDefinition PaymentsErrorAllocations = new("BF_PAYMENTS_ERROR_ALLOCATIONS", "1–100 farklı borç ve geçerli tutar/sürüm seçin.", "error");
    public static readonly ErrorDefinition DuesErrorUnitSelection = new("BF_DUES_ERROR_UNIT_SELECTION", "1–500 farklı bağımsız bölüm seçin.", "error");
    public static readonly ErrorDefinition DuesErrorPeriod = new("BF_DUES_ERROR_PERIOD", "2000 veya sonrası için YYYY-MM biçiminde dönem girin.", "error");
    public static readonly ErrorDefinition ApartmentNoBuilding = new("BF_APARTMENT_NO_BUILDING", "Apartmana blok eklenmez; doğrudan daire ekleyin.", "warning");
    public static readonly ErrorDefinition ApartmentDirectUnitsOnly = new("BF_APARTMENT_DIRECT_UNITS_ONLY", "Apartmanda yalnızca doğrudan daire kaydı eklenebilir.", "warning");
    public static readonly ErrorDefinition ErrorsSearchBounds = new("BF_ERRORS_SEARCH_BOUNDS", "Arama en fazla 200 karakter olabilir.", "error");
    public static readonly ErrorDefinition UnitNumberExists = new("BF_UNIT_NUMBER_EXISTS", "Aynı kapsamda bu bağımsız bölüm numarası zaten var.", "warning");
    public static readonly ErrorDefinition DuesErrorDescription = new("BF_DUES_ERROR_DESCRIPTION", "Açıklama zorunludur ve en fazla 500 karakter olabilir.", "error");
    public static readonly ErrorDefinition UiSelectAUnit = new("BF_UI_SELECT_AUNIT", "Bağımsız bölüm seçin.", "error");
    public static readonly ErrorDefinition UiAStartDateIsRequired = new("BF_UI_ASTART_DATE_IS_REQUIRED", "Başlangıç tarihi zorunludur.", "error");
    public static readonly ErrorDefinition PropertyTypeLocked = new("BF_PROPERTY_TYPE_LOCKED", "Bina/blok veya bağımsız bölüm kaydı ya da geçmişi bulunduğu için yönetim türü değiştirilemez.", "warning");
    public static readonly ErrorDefinition UiTheEndDateCannotPrecedeTheStartDate = new("BF_UI_THE_END_DATE_CANNOT_PRECEDE_THE_START_DATE", "Bitiş tarihi başlangıçtan önce olamaz.", "error");
    public static readonly ErrorDefinition OccupancyAlreadyEnded = new("BF_OCCUPANCY_ALREADY_ENDED", "Bitiş tarihi kaydedilmiş; ilişki geçmişi yeniden yazılamaz.", "warning");
    public static readonly ErrorDefinition EndDateRequired = new("BF_END_DATE_REQUIRED", "Bitiş tarihi zorunludur.", "error");
    public static readonly ErrorDefinition BuildingHasUnits = new("BF_BUILDING_HAS_UNITS", "Bloğu silmeden önce tüm dairelerini silin.", "warning");
    public static readonly ErrorDefinition ErrorsAreaBounds = new("BF_ERRORS_AREA_BOUNDS", "Brüt alan 0'dan büyük, en fazla 99999999,99 m² ve en fazla iki ondalık basamak olmalı.", "error");
    public static readonly ErrorDefinition BuildingUnitNumberExists = new("BF_BUILDING_UNIT_NUMBER_EXISTS", "Bu blokta aynı daire numarası zaten var.", "warning");
    public static readonly ErrorDefinition OccupancyOverlap = new("BF_OCCUPANCY_OVERLAP", "Bu bölümde çakışan kişi veya birincil iletişim ilişkisi zaten var.", "warning");
    public static readonly ErrorDefinition DuesErrorCurrency = new("BF_DUES_ERROR_CURRENCY", "Bu fazda yalnızca TRY desteklenir.", "error");
    public static readonly ErrorDefinition PaymentsErrorUnique = new("BF_PAYMENTS_ERROR_UNIQUE", "Bu havale referansı veya ödeme isteği zaten kayıtlı. Listeyi kontrol edin.", "warning");
    public static readonly ErrorDefinition DuesErrorRequestConflict = new("BF_DUES_ERROR_REQUEST_CONFLICT", "Bu istek kimliği farklı bir tahakkuk için kullanılmış.", "warning");
    public static readonly ErrorDefinition PaymentsErrorRequestConflict = new("BF_PAYMENTS_ERROR_REQUEST_CONFLICT", "Bu istek kimliği farklı bir ödeme için kullanılmış.", "warning");
    public static readonly ErrorDefinition PersonOccupancyOverlap = new("BF_PERSON_OCCUPANCY_OVERLAP", "Bu kişinin aynı bölüm ve roldeki tarihleri çakışıyor.", "warning");
    public static readonly ErrorDefinition BuildingNameExists = new("BF_BUILDING_NAME_EXISTS", "Bu sitede aynı blok adı zaten var.", "warning");
    public static readonly ErrorDefinition PrimaryContactOverlap = new("BF_PRIMARY_CONTACT_OVERLAP", "Bu tarihlerde bölümün birincil iletişim kişisi zaten var.", "warning");
    public static readonly ErrorDefinition BuildingRequired = new("BF_BUILDING_REQUIRED", "Bu yönetim alanında önce bina/blok seçin.", "warning");
    public static readonly ErrorDefinition DuesErrorDebtor = new("BF_DUES_ERROR_DEBTOR", "Daire, kiracı veya malik muhatabı seçin.", "error");
    public static readonly ErrorDefinition PaymentsErrorBalance = new("BF_PAYMENTS_ERROR_BALANCE", "Dağıtılan tutar borcun kalan bakiyesini aşamaz; iptal borcuna ödeme yapılamaz.", "warning");
    public static readonly ErrorDefinition PersonHasOccupancy = new("BF_PERSON_HAS_OCCUPANCY", "Devam eden veya planlı ilişkileri bitirmeden kişiyi pasifleştiremezsiniz.", "warning");
    public static readonly ErrorDefinition PersonDeleteHasOccupancy = new("BF_PERSON_DELETE_HAS_OCCUPANCY", "Devam eden veya planlı ilişkileri bitirmeden kişiyi silemezsiniz.", "warning");
    public static readonly ErrorDefinition UnitHasOccupancy = new("BF_UNIT_HAS_OCCUPANCY", "Devam eden veya planlı kişi ilişkileri bitirilmeden bölüm pasifleştirilemez.", "warning");
    public static readonly ErrorDefinition UnitDeleteHasOccupancy = new("BF_UNIT_DELETE_HAS_OCCUPANCY", "Devam eden veya planlı kişi ilişkileri bitirilmeden bölüm silinemez.", "warning");
    public static readonly ErrorDefinition DuesOtherReasonRequired = new("BF_DUES_OTHER_REASON_REQUIRED", "Diğer iptal nedeni için açıklama zorunludur.", "error");
    public static readonly ErrorDefinition DuesErrorApproval = new("BF_DUES_ERROR_APPROVAL", "En fazla 500 farklı borcu güncel sürümüyle onaylayın.", "error");
    public static readonly ErrorDefinition EnabledRequired = new("BF_ENABLED_REQUIRED", "Etkinlik durumu zorunludur.", "error");
    public static readonly ErrorDefinition DuesErrorPropertyHistory = new("BF_DUES_ERROR_PROPERTY_HISTORY", "Finans geçmişi bulunan yönetim alanı silinemez. Geçmişi korumak için pasife alabilirsiniz.", "warning");
    public static readonly ErrorDefinition DuesErrorUnit = new("BF_DUES_ERROR_UNIT", "Geçerli bağımsız bölüm seçin.", "error");
    public static readonly ErrorDefinition UiEnterAValidEmailAddress = new("BF_UI_ENTER_AVALID_EMAIL_ADDRESS", "Geçerli bir e-posta adresi girin.", "error");
    public static readonly ErrorDefinition ErrorsValidPropertyType = new("BF_ERRORS_VALID_PROPERTY_TYPE", "Geçerli bir yönetim alanı türü seçin.", "error");
    public static readonly ErrorDefinition DuesErrorStatus = new("BF_DUES_ERROR_STATUS", "Geçerli borç durumu seçin.", "error");
    public static readonly ErrorDefinition DuesErrorRequestId = new("BF_DUES_ERROR_REQUEST_ID", "Geçerli istek kimliği zorunludur.", "error");
    public static readonly ErrorDefinition VersionRequired = new("BF_VERSION_REQUIRED", "Geçerli kayıt sürümü zorunludur.", "error");
    public static readonly ErrorDefinition DuesErrorPersonChoices = new("BF_DUES_ERROR_PERSON_CHOICES", "Geçerli kişi araması ve sayfa seçin.", "error");
    public static readonly ErrorDefinition PersonInvalid = new("BF_PERSON_INVALID", "Geçerli kişi seçin.", "error");
    public static readonly ErrorDefinition DuesErrorChoices = new("BF_DUES_ERROR_CHOICES", "Geçerli sayfa ve bağımsız bölüm araması girin.", "error");
    public static readonly ErrorDefinition PaymentsErrorStatus = new("BF_PAYMENTS_ERROR_STATUS", "Geçerli ödeme durumu seçin.", "error");
    public static readonly ErrorDefinition PaymentsErrorDate = new("BF_PAYMENTS_ERROR_DATE", "Geçerli ödeme tarihi zorunludur.", "error");
    public static readonly ErrorDefinition InvalidPage = new("BF_INVALID_PAGE", "Geçersiz sayfa.", "error");
    public static readonly ErrorDefinition InvalidSortField = new("BF_INVALID_SORT_FIELD", "Geçersiz sıralama alanı.", "error");
    public static readonly ErrorDefinition InvalidSortDirection = new("BF_INVALID_SORT_DIRECTION", "Geçersiz sıralama yönü.", "error");
    public static readonly ErrorDefinition PaymentsReferenceRequired = new("BF_PAYMENTS_REFERENCE_REQUIRED", "Havalede referans zorunludur; en fazla 100 karakter girin.", "error");
    public static readonly ErrorDefinition DuesErrorAmbiguousDebtor = new("BF_DUES_ERROR_AMBIGUOUS_DEBTOR", "Her bölümde dönem başında tek geçerli muhatap olmalı; ilişkileri kontrol edin veya daire muhatabı seçin.", "error");
    public static readonly ErrorDefinition BuildingFloorMetadata = new("BF_BUILDING_FLOOR_METADATA", "Kat sayısı bu türde bina/blok üzerinde tutulur.", "warning");
    public static readonly ErrorDefinition UiTheNumberOfFloorsMustBePositive = new("BF_UI_THE_NUMBER_OF_FLOORS_MUST_BE_POSITIVE", "Kat sayısı pozitif olmalı.", "error");
    public static readonly ErrorDefinition ErrorsApartmentFloorsOnly = new("BF_ERRORS_APARTMENT_FLOORS_ONLY", "Kat sayısı yalnızca apartman için girilebilir.", "error");
    public static readonly ErrorDefinition ErrorsApartmentPositiveFloors = new("BF_ERRORS_APARTMENT_POSITIVE_FLOORS", "Kat sayısı yalnızca apartman için pozitif olmalı.", "error");
    public static readonly ErrorDefinition ErrorsFloorRequired = new("BF_ERRORS_FLOOR_REQUIRED", "Kat zorunludur; bodrum için negatif değer kullanılabilir.", "error");
    public static readonly ErrorDefinition RecordChanged = new("BF_RECORD_CHANGED", "Kayıt değişti. Güncel bilgileri yükleyip yeniden deneyin.", "warning");
    public static readonly ErrorDefinition DuesErrorMoveOutReason = new("BF_DUES_ERROR_MOVE_OUT_REASON", "Kiracı taşınması iptal nedeni kullanılamıyor. Önizlemeyi yenileyin.", "warning");
    public static readonly ErrorDefinition PersonRequired = new("BF_PERSON_REQUIRED", "Kişi seçin.", "error");
    public static readonly ErrorDefinition OccupancyTypeInvalid = new("BF_OCCUPANCY_TYPE_INVALID", "Malik, kiracı veya sakin seçin.", "error");
    public static readonly ErrorDefinition PaymentsErrorMethod = new("BF_PAYMENTS_ERROR_METHOD", "Nakit veya banka havalesi seçin.", "error");
    public static readonly ErrorDefinition DuesErrorStaleApproval = new("BF_DUES_ERROR_STALE_APPROVAL", "Onaylanan borç değişti veya iptale uygun değil. Önizlemeyi yenileyin.", "warning");
    public static readonly ErrorDefinition DuesErrorInactive = new("BF_DUES_ERROR_INACTIVE", "Pasif kayıtta tahakkuk veya iptal yapılamaz.", "warning");
    public static readonly ErrorDefinition PaymentsInactiveProperty = new("BF_PAYMENTS_INACTIVE_PROPERTY", "Pasif yönetim alanında ödeme kaydı veya iptali yapılamaz.", "warning");
    public static readonly ErrorDefinition ErrorsPageBounds = new("BF_ERRORS_PAGE_BOUNDS", "Sayfa 1 ile 1000000 arasında olmalı.", "error");
    public static readonly ErrorDefinition ErrorsPageSizeBounds = new("BF_ERRORS_PAGE_SIZE_BOUNDS", "Sayfa boyutu 1 ile 100 arasında olmalı.", "error");
    public static readonly ErrorDefinition PageSizeBounds = new("BF_PAGE_SIZE_BOUNDS", "Sayfa boyutu 1–100 olmalı.", "error");
    public static readonly ErrorDefinition DuesErrorDuplicate = new("BF_DUES_ERROR_DUPLICATE", "Seçilen bölümlerden birinde bu dönem için aidat zaten var. Hiçbir yeni borç kaydedilmedi.", "warning");
    public static readonly ErrorDefinition DuesErrorBuilding = new("BF_DUES_ERROR_BUILDING", "Seçilen bölümün bina/blok kaydı artık kullanılamıyor.", "warning");
    public static readonly ErrorDefinition DuesErrorDueRange = new("BF_DUES_ERROR_DUE_RANGE", "Son vade ilk vadeden önce olamaz.", "error");
    public static readonly ErrorDefinition PaymentsErrorDateRange = new("BF_PAYMENTS_ERROR_DATE_RANGE", "Son ödeme tarihi ilk tarihten önce olamaz.", "error");
    public static readonly ErrorDefinition DuesErrorUnique = new("BF_DUES_ERROR_UNIQUE", "Tahakkuk isteği veya daire/dönem kaydı zaten var. Listeyi yenileyin.", "warning");
    public static readonly ErrorDefinition PhoneTooLong = new("BF_PHONE_TOO_LONG", "Telefon en fazla 30 karakter olabilir.", "error");
    public static readonly ErrorDefinition DuesErrorAmount = new("BF_DUES_ERROR_AMOUNT", "Tutar 0'dan büyük, en fazla 99999999,99 ve iki ondalık basamak olmalı.", "error");
    public static readonly ErrorDefinition DuesErrorDue = new("BF_DUES_ERROR_DUE", "Vade zorunludur ve tahakkuk döneminden önce olamaz.", "error");
    public static readonly ErrorDefinition PropertyHasChildren = new("BF_PROPERTY_HAS_CHILDREN", "Yönetim alanını silmeden önce tüm bina/blok, bağımsız bölüm ve kişi kayıtlarını silin.", "warning");
    public static readonly ErrorDefinition PaymentsErrorFutureDate = new("BF_PAYMENTS_ERROR_FUTURE_DATE", "Ödeme tarihi gelecekte olamaz.", "error");
    public static readonly ErrorDefinition PaymentsErrorTotal = new("BF_PAYMENTS_ERROR_TOTAL", "Ödeme toplamı borçlara dağıtılan tutarla eşit olmalı.", "error");
    public static readonly ErrorDefinition PaymentsDebtHasPayments = new("BF_PAYMENTS_DEBT_HAS_PAYMENTS", "Ödemeli borç iptal edilemez; önce ilgili ödemeleri iptal edin.", "warning");
    public static readonly ErrorDefinition OccupancyParentInactive = new("BF_OCCUPANCY_PARENT_INACTIVE", "Üst kayıt pasif. Önce yönetim alanını, binayı, bölümü ve kişiyi aktifleştirin.", "warning");
    public static readonly ErrorDefinition ParentInactive = new("BF_PARENT_INACTIVE", "Üst kayıt pasif. İşlem yapmak için önce siteyi ve bloğu aktifleştirin.", "warning");
    public static readonly ErrorDefinition DuesErrorCancelled = new("BF_DUES_ERROR_CANCELLED", "İptal edilmiş borcun geçmişi yeniden yazılamaz.", "warning");
    public static readonly ErrorDefinition PaymentsErrorCancelled = new("BF_PAYMENTS_ERROR_CANCELLED", "İptal edilmiş ödeme geçmişi yeniden yazılamaz.", "warning");
    public static readonly ErrorDefinition DuesErrorReason = new("BF_DUES_ERROR_REASON", "İptal gerekçesi zorunludur ve en fazla 500 karakter olabilir.", "error");
    public static readonly ErrorDefinition DuesChooseReason = new("BF_DUES_CHOOSE_REASON", "İptal nedeni seçin.", "error");
    public static readonly ErrorDefinition ValidationFailed = new("BF_VALIDATION_FAILED", "Alanları kontrol edin.", "error");
    public static readonly ErrorDefinition InvalidRequest = new("BF_INVALID_REQUEST", "Geçersiz istek.", "error");
    public static readonly ErrorDefinition SignInRequired = new("BF_SIGN_IN_REQUIRED", "Oturumunuzu yenileyip tekrar giriş yapın.", "error");
    public static readonly ErrorDefinition AccessDenied = new("BF_ACCESS_DENIED", "Bu işlem için yetkiniz yok.", "error");
    public static readonly ErrorDefinition ResourceNotFound = new("BF_RESOURCE_NOT_FOUND", "Kayıt bulunamadı veya bu kayda erişiminiz yok.", "error");
    public static readonly ErrorDefinition UnexpectedFailure = new("BF_UNEXPECTED_FAILURE", "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.", "error");
    public static readonly ErrorDefinition MethodNotAllowed = new("BF_METHOD_NOT_ALLOWED", "Bu işlem yöntemi desteklenmiyor.", "error");
    public static readonly ErrorDefinition UnsupportedMedia = new("BF_UNSUPPORTED_MEDIA", "İstek içerik türü desteklenmiyor.", "error");
    public static readonly ErrorDefinition RateLimited = new("BF_RATE_LIMITED", "Çok fazla istek yapıldı. Biraz bekleyip tekrar deneyin.", "error");
    public static readonly ErrorDefinition FieldRequired = new("BF_FIELD_REQUIRED", "{field} zorunludur.", "error", "field");
    public static readonly ErrorDefinition FieldTooLong = new("BF_FIELD_TOO_LONG", "{field} en fazla {max} karakter olabilir.", "error", "field", "max");
    public static readonly ErrorDefinition FieldRequiredMax = new("BF_FIELD_REQUIRED_MAX", "{field} zorunludur ve en fazla {max} karakter olabilir.", "error", "field", "max");

    public static IReadOnlyDictionary<string, string> FieldLabels { get; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
    {
        ["propertyName"] = "Yönetim alanı adı",
        ["city"] = "Şehir",
        ["district"] = "İlçe",
        ["address"] = "Adres",
        ["neighborhood"] = "Mahalle/semt",
        ["buildingName"] = "Bina/blok adı",
        ["unitNumber"] = "Daire numarası",
        ["firstName"] = "Ad",
        ["lastName"] = "Soyad",
        ["phoneNumber"] = "Telefon",
    });
    public static string FieldKey(string label) => label == "Blok adı" ? "buildingName" : FieldLabels.First(value => value.Value == label).Key;
    public static IReadOnlyList<ErrorDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        ConstructionYearInvalid,
        HeatingTypeInvalid,
        ParkingTypeInvalid,
        BuildingFeaturesApartmentOnly,
        NetAreaInvalid,
        RoomLayoutInvalid,
        FloorPositionInvalid,
        FloorExceedsFloorCount,
        BathroomCountInvalid,
        KitchenTypeInvalid,
        ContentInvalid,
        ContentState,
        DocumentInvalid,
        DocumentTooLarge,
        DocumentRequestConflict,
        DocumentLimit,
        StorageUnavailable,
        ContentPropertyHistory,
        ExpenseCategoryInvalid,
        ExpenseDateInvalid,
        ExpenseReferenceInvalid,
        BudgetAmountInvalid,
        ExpenseRequestConflict,
        ExpenseAlreadyCancelled,
        BudgetRequestConflict,

        PaymentsErrorAllocations,
        DuesErrorUnitSelection,
        DuesErrorPeriod,
        ApartmentNoBuilding,
        ApartmentDirectUnitsOnly,
        ErrorsSearchBounds,
        UnitNumberExists,
        DuesErrorDescription,
        UiSelectAUnit,
        UiAStartDateIsRequired,
        PropertyTypeLocked,
        UiTheEndDateCannotPrecedeTheStartDate,
        OccupancyAlreadyEnded,
        EndDateRequired,
        BuildingHasUnits,
        ErrorsAreaBounds,
        BuildingUnitNumberExists,
        OccupancyOverlap,
        DuesErrorCurrency,
        PaymentsErrorUnique,
        DuesErrorRequestConflict,
        PaymentsErrorRequestConflict,
        PersonOccupancyOverlap,
        BuildingNameExists,
        PrimaryContactOverlap,
        BuildingRequired,
        DuesErrorDebtor,
        PaymentsErrorBalance,
        PersonHasOccupancy,
        PersonDeleteHasOccupancy,
        UnitHasOccupancy,
        UnitDeleteHasOccupancy,
        DuesOtherReasonRequired,
        DuesErrorApproval,
        EnabledRequired,
        DuesErrorPropertyHistory,
        DuesErrorUnit,
        UiEnterAValidEmailAddress,
        ErrorsValidPropertyType,
        DuesErrorStatus,
        DuesErrorRequestId,
        VersionRequired,
        DuesErrorPersonChoices,
        PersonInvalid,
        DuesErrorChoices,
        PaymentsErrorStatus,
        PaymentsErrorDate,
        InvalidPage,
        InvalidSortField,
        InvalidSortDirection,
        PaymentsReferenceRequired,
        DuesErrorAmbiguousDebtor,
        BuildingFloorMetadata,
        UiTheNumberOfFloorsMustBePositive,
        ErrorsApartmentFloorsOnly,
        ErrorsApartmentPositiveFloors,
        ErrorsFloorRequired,
        RecordChanged,
        DuesErrorMoveOutReason,
        PersonRequired,
        OccupancyTypeInvalid,
        PaymentsErrorMethod,
        DuesErrorStaleApproval,
        DuesErrorInactive,
        PaymentsInactiveProperty,
        ErrorsPageBounds,
        ErrorsPageSizeBounds,
        PageSizeBounds,
        DuesErrorDuplicate,
        DuesErrorBuilding,
        DuesErrorDueRange,
        PaymentsErrorDateRange,
        DuesErrorUnique,
        PhoneTooLong,
        DuesErrorAmount,
        DuesErrorDue,
        PropertyHasChildren,
        PaymentsErrorFutureDate,
        PaymentsErrorTotal,
        PaymentsDebtHasPayments,
        OccupancyParentInactive,
        ParentInactive,
        DuesErrorCancelled,
        PaymentsErrorCancelled,
        DuesErrorReason,
        DuesChooseReason,
        ValidationFailed,
        InvalidRequest,
        SignInRequired,
        AccessDenied,
        ResourceNotFound,
        UnexpectedFailure,
        MethodNotAllowed,
        UnsupportedMedia,
        RateLimited,
        FieldRequired,
        FieldTooLong,
        FieldRequiredMax,
    });
}
