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
    public static readonly ErrorDefinition CommonAreaMaintenanceInvalid=new("BF_COMMON_AREA_MAINTENANCE_INVALID","Geçerli bakım planı, tarih, saat aralığı ve görünür duyuru metni girin.","error");
    public static readonly ErrorDefinition CommonAreaMaintenanceConflict=new("BF_COMMON_AREA_MAINTENANCE_CONFLICT","Bakım kapanışı rezervasyon veya başka bir kayıtla çakışıyor. Güncel planı kontrol edin.","warning");
    public static readonly ErrorDefinition CommonAreaReservationClosed = new("BF_COMMON_AREA_RESERVATION_CLOSED", "Alan bu tarih ve saatte rezervasyona açık değil veya rezervasyon artık değiştirilemez.", "warning");
    public static readonly ErrorDefinition CommonAreaReservationFull = new("BF_COMMON_AREA_RESERVATION_FULL", "Seçilen saat aralığında yeterli kapasite yok. Saat takvimini kontrol edin.", "warning");
    public static readonly ErrorDefinition CommonAreaReservationInvalid = new("BF_COMMON_AREA_RESERVATION_INVALID", "Rezervasyon tarihini, 15 dakikalık saat aralığını, kişi sayısını ve daireyi kontrol edin.", "error");
    public static readonly ErrorDefinition CommonAreaInvalid = new("BF_COMMON_AREA_INVALID", "Ortak alan bilgilerini, kapsamı, çalışma saatlerini ve ücretini kontrol edin.", "error");
    public static readonly ErrorDefinition CommonAreaConflict = new("BF_COMMON_AREA_CONFLICT", "Ortak alan isteği veya bağlı kayıt geçmişi çakışıyor. Güncel kaydı kontrol edin.", "warning");
    public static readonly ErrorDefinition WebsiteDomainInvalid=new("BF_WEBSITE_DOMAIN_INVALID","Alan adı, tür, istek kimliği ve sürümü kontrol edin.","error");
    public static readonly ErrorDefinition WebsiteDomainConflict=new("BF_WEBSITE_DOMAIN_CONFLICT","Alan adı başka bir site için ayrılmış.","warning");
    public static readonly ErrorDefinition WebsiteDomainUnverified=new("BF_WEBSITE_DOMAIN_UNVERIFIED","Güncel DNS/TLS doğrulaması ve gerekli onay tamamlanmadı.","warning");
    public static readonly ErrorDefinition WebsitePreviewUnavailable=new("BF_WEBSITE_PREVIEW_UNAVAILABLE","Güvenli önizleme anahtarı henüz yapılandırılmadı.","warning");

    public static readonly ErrorDefinition InquiryRejected = new("BF_INQUIRY_REJECTED", "Başvuru bilgilerini kontrol edin; aydınlatma metnini yenileyip tekrar deneyin.", "error");
    public static readonly ErrorDefinition InquiryUnavailable = new("BF_INQUIRY_UNAVAILABLE", "Başvuru formu henüz yapılandırılmadı.", "warning");
    public static readonly ErrorDefinition ListingInvalid = new("BF_LISTING_INVALID", "İlan alanlarını, fotoğrafları ve aynı alandaki aktif bağımsız bölümü kontrol edin.", "error");
    public static readonly ErrorDefinition ListingState = new("BF_LISTING_STATE_INVALID", "İlan bu işlem için uygun durumda değil; güncel kaydı kontrol edin.", "warning");
    public static readonly ErrorDefinition ListingConsent = new("BF_LISTING_CONSENT_REQUIRED", "İlan için özel malik yetki belgesi gerekli.", "warning");
    public static readonly ErrorDefinition ListingCompliance = new("BF_LISTING_COMPLIANCE_REQUIRED", "Mevzuat kontrolünü açıkça onaylayın ve inceleme notu girin.", "warning");
    public static readonly ErrorDefinition ListingVerification = new("BF_LISTING_VERIFICATION_REQUIRED", "EİDS kimlik ve yetki doğrulama sağlayıcısı bağlı değil veya yetki geçerli değil; yayın kapalı.", "warning");

    public static readonly ErrorDefinition PersonProfileInvalid = new("BF_PERSON_PROFILE_INVALID", "Kişi türü, uyruk, ülke, kimlik ve zorunlu iletişim alanlarını kontrol edin.", "error");
    public static readonly ErrorDefinition PersonAccountInvalid = new("BF_PERSON_ACCOUNT_INVALID", "Aynı yönetim alanında aktif üyeliği olan giriş hesabını seçin.", "error");
    public static readonly ErrorDefinition PersonMessageInvalid = new("BF_PERSON_MESSAGE_INVALID", "Başlık, mesaj, istek kimliği ve desteklenen bildirim kanalı gerekli.", "error");
    public static readonly ErrorDefinition PersonMessageConflict = new("BF_PERSON_MESSAGE_CONFLICT", "Mesaj isteği veya kişi sürümü değişti. Güncel kaydı kontrol edin.", "warning");
    public static readonly ErrorDefinition SmsInvalid = new("BF_SMS_INVALID", "SMS alanlarını kontrol edin.", "error");
    public static readonly ErrorDefinition SmsPhoneInvalid = new("BF_SMS_PHONE_INVALID", "Geçerli bir cep telefonu numarası girin.", "error");
    public static readonly ErrorDefinition SmsTemplateParameterInvalid = new("BF_SMS_TEMPLATE_PARAMETER_INVALID", "Şablon yalnız bu SMS tipinin parametrelerini kullanabilir.", "error");
    public static readonly ErrorDefinition SmsAccountRequired = new("BF_SMS_ACCOUNT_REQUIRED", "Bu yönetim alanında etkin bir SMS hesabı yok.", "warning");
    public static readonly ErrorDefinition SmsCreditInsufficient = new("BF_SMS_CREDIT_INSUFFICIENT", "SMS kontörü yetersiz.", "warning");
    public static readonly ErrorDefinition SmsTemplateConflict = new("BF_SMS_TEMPLATE_CONFLICT", "Bu SMS tipi için zaten bir şablon var.", "warning");
    public static readonly ErrorDefinition SmsPhoneNotVerified = new("BF_SMS_PHONE_NOT_VERIFIED", "Pazarlama SMS izni için önce telefon numaranızı doğrulayın.", "warning");
    public static readonly ErrorDefinition ConsentInvalid = new("BF_CONSENT_INVALID", "Rıza alanlarını kontrol edin.", "error");
    public static readonly ErrorDefinition ConsentTextInvalid = new("BF_CONSENT_TEXT_INVALID", "Metin boş, çok uzun ya da gizli/kontrol karakteri içeriyor.", "error");
    public static readonly ErrorDefinition ConsentLinkInvalid = new("BF_CONSENT_LINK_INVALID", "Metindeki bağlantılar yalnız https:// veya mailto: ile başlayabilir.", "error");
    public static readonly ErrorDefinition ConsentTypeConflict = new("BF_CONSENT_TYPE_CONFLICT", "Bu kodla bir rıza türü zaten var.", "warning");
    public static readonly ErrorDefinition ConsentSystemType = new("BF_CONSENT_SYSTEM_TYPE", "Sistem rıza türü silinemez veya pasife alınamaz.", "warning");
    public static readonly ErrorDefinition ConsentTypeInUse = new("BF_CONSENT_TYPE_IN_USE", "Yayınlanmış metni olan rıza türü silinemez; pasife alabilirsiniz.", "warning");
    public static readonly ErrorDefinition ConsentOperationConflict = new("BF_CONSENT_OPERATION_CONFLICT", "Aynı istek kimliği farklı içerikle kullanıldı. Sayfayı yenileyip yeniden deneyin.", "warning");
    public static readonly ErrorDefinition ConsentTypeDisabled = new("BF_CONSENT_TYPE_DISABLED", "Pasif rıza türüne yeni metin yayınlanamaz.", "warning");
    public static readonly ErrorDefinition ConsentRequired = new("BF_CONSENT_REQUIRED", "Fotoğraf eklemek için güncel açık rıza metnini onaylayın.", "warning");
    public static readonly ErrorDefinition ConsentTextOutdated = new("BF_CONSENT_TEXT_OUTDATED", "Rıza metni güncellendi. Güncel metni okuyup yeniden onaylayın.", "warning");
    public static readonly ErrorDefinition ConsentNotWithdrawable = new("BF_CONSENT_NOT_WITHDRAWABLE", "Aydınlatma metni okundu bilgisi geri çekilemez.", "warning");
    public static readonly ErrorDefinition UserInvalid = new("BF_USER_INVALID", "Kullanıcı adı (küçük harf, 3–50), e-posta, ad, soyad ve girilmişse cep telefonunu kontrol edin.", "error");
    public static readonly ErrorDefinition UserPasswordWeak = new("BF_USER_PASSWORD_WEAK", "Geçici parola en az 12 karakter olmalı; büyük harf, küçük harf ve rakam içermeli.", "error");
    public static readonly ErrorDefinition UserExists = new("BF_USER_EXISTS", "Bu kullanıcı adı veya e-posta zaten kayıtlı.", "warning");
    public static readonly ErrorDefinition UserRoleForbidden = new("BF_USER_ROLE_FORBIDDEN", "Bu rolü atama yetkiniz yok.", "error");
    public static readonly ErrorDefinition UserSelfChange = new("BF_USER_SELF_CHANGE", "Kendi hesabınızı pasifleştiremez veya yönetici rolünüzü kaldıramazsınız.", "error");
    public static readonly ErrorDefinition UserLinkConflict = new("BF_USER_LINK_CONFLICT", "Kişi başka bir kullanıcıya bağlı ya da kullanıcı bu yönetim alanında başka bir kişiye bağlı.", "warning");
    public static readonly ErrorDefinition IdentityUnavailable = new("BF_IDENTITY_UNAVAILABLE", "Kimlik sunucusuna şu anda erişilemiyor. Lütfen tekrar deneyin.", "error");
    public static readonly ErrorDefinition MobileRegistrationInvalid = new("BF_MOBILE_REGISTRATION_INVALID", "Kayıt bilgilerini kontrol edin.", "error");
    public static readonly ErrorDefinition MobileRegistrationConflict = new("BF_MOBILE_REGISTRATION_CONFLICT", "Bu bilgilerle kayıt oluşturulamadı. Farklı bir kullanıcı adı veya e-posta deneyin ya da giriş yapın.", "warning");
    public static readonly ErrorDefinition MobileRateLimited = new("BF_MOBILE_RATE_LIMITED", "Çok fazla deneme yapıldı. Bir süre sonra tekrar deneyin.", "warning");
    public static readonly ErrorDefinition MobileLinkRejected = new("BF_MOBILE_LINK_REJECTED", "Kişi kodu veya girdiğiniz bilgiler kayıtla uyuşmuyor.", "warning");
    public static readonly ErrorDefinition MobileLinkLocked = new("BF_MOBILE_LINK_LOCKED", "Çok fazla hatalı deneme yapıldı. 15 dakika sonra tekrar deneyin.", "warning");
    public static readonly ErrorDefinition PersonCodeUnavailable = new("BF_PERSON_CODE_UNAVAILABLE", "Kişi kodu şu anda üretilemedi. Kaydı yenileyip tekrar deneyin.", "warning");

    public static readonly ErrorDefinition MeterInvalid = new("BF_METER_INVALID", "Geçerli sayaç adı, türü ve istek kimliği gerekli.", "error");
    public static readonly ErrorDefinition ReadingInvalid = new("BF_READING_INVALID", "2000 veya sonrası, gelecekte olmayan sabit tarih; sıfır veya pozitif en fazla üç ondalıklı endeks, gerekçe ve geçerli sürüm gerekli.", "error");
    public static readonly ErrorDefinition MeterConflict = new("BF_METER_CONFLICT", "Sayaç adı, okuma tarihi veya istek kimliği çakışıyor. Güncel listeyi kontrol edin.", "warning");
    public static readonly ErrorDefinition MeterInvalidUnit = new("BF_METER_INVALID_UNIT", "Aynı yönetim alanındaki aktif bağımsız bölümü seçin.", "warning");
    public static readonly ErrorDefinition MeterInactive = new("BF_METER_INACTIVE", "Pasif sayaca yeni okuma veya düzeltme eklenemez.", "warning");
    public static readonly ErrorDefinition SurveyInvalid = new("BF_SURVEY_INVALID", "Anket için 1–200 karakter başlık, geçerli kategori, hedef kitle, yanıt kuralı ve başlangıçtan önce olmayan bitiş tarihi gerekli; açıklama en fazla 2000 karakter olabilir.", "error");
    public static readonly ErrorDefinition SurveyQuestionInvalid = new("BF_SURVEY_QUESTION_INVALID", "En fazla 30 soru eklenebilir. Her soru 1–500 karakter olmalı; seçmeli sorularda 2–10 farklı seçenek (en fazla 200 karakter) bulunmalı, puan ve metin sorularında seçenek olmamalı.", "error");
    public static readonly ErrorDefinition SurveyState = new("BF_SURVEY_STATE", "Anket bu işlem için uygun durumda değil. Yalnız taslak düzenlenip silinebilir; yayın için en az bir soru ve geçmemiş bitiş tarihi gerekir.", "warning");
    public static readonly ErrorDefinition SurveyAnswerInvalid = new("BF_SURVEY_ANSWER_INVALID", "Yanıtları kontrol edin: zorunlu soruları cevaplayın, yalnız sorunun seçeneklerini seçin, puanı 1–5 arasında verin ve metni en fazla 1000 karakter yazın.", "error");
    public static readonly ErrorDefinition SurveyAlreadyAnswered = new("BF_SURVEY_ALREADY_ANSWERED", "Bu anket için yanıtınız zaten kaydedildi; yanıtlar sonradan değiştirilemez.", "warning");
    public static readonly ErrorDefinition SurveyNotOpen = new("BF_SURVEY_NOT_OPEN", "Anket şu anda yanıta açık değil.", "warning");
    public static readonly ErrorDefinition SupplierPropertyHistory = new("BF_SUPPLIER_PROPERTY_HISTORY", "Tedarikçi hizmet veya sözleşme geçmişi olan yönetim alanı silinemez.", "warning");
    public static readonly ErrorDefinition SupplierContractState = new("BF_SUPPLIER_CONTRACT_STATE", "Sözleşme durumu, yenileme aralığı veya değişmez alanları bu işleme uygun değil.", "warning");
    public static readonly ErrorDefinition SupplierQuoteState = new("BF_SUPPLIER_QUOTE_STATE", "Teklifin mevcut durumu bu işleme uygun değil.", "warning");
    public static readonly ErrorDefinition SupplierWorkOrderState = new("BF_SUPPLIER_WORK_ORDER_STATE", "İş emrinin durumu, kaynak bağlantısı veya fotoğraf sınırı bu işleme uygun değil.", "warning");
    public static readonly ErrorDefinition SupplierMaintenanceState = new("BF_SUPPLIER_MAINTENANCE_STATE", "Bakım planının, sözleşmenin veya bağlı iş emrinin durumu bu işleme uygun değil.", "warning");
    public static readonly ErrorDefinition SupplierMaintenanceConflict = new("BF_SUPPLIER_MAINTENANCE_CONFLICT", "Planlanan bakım tarihleri aynı hizmetin başka bir bakımıyla çakışıyor veya dönem içinde iş günü bulunamıyor.", "warning");
    public static readonly ErrorDefinition SupplierInvoiceDuplicate = new("BF_SUPPLIER_INVOICE_DUPLICATE", "Bu tedarikçi için aynı numaralı geçerli bir fatura zaten kayıtlı.", "warning");
    public static readonly ErrorDefinition SupplierInvoiceState = new("BF_SUPPLIER_INVOICE_STATE", "Faturanın veya bağlı iş emri, sözleşme ya da düzeltilen faturanın durumu bu işleme uygun değil.", "warning");
    public static readonly ErrorDefinition SupplierInvoiceLinked = new("BF_SUPPLIER_INVOICE_LINKED", "Tedarikçi faturasına bağlı gider yalnız fatura üzerinden iptal edilebilir.", "warning");
    public static readonly ErrorDefinition SupplierDocumentProtected = new("BF_SUPPLIER_DOCUMENT_PROTECTED", "Revizyon veya sözleşme geçmişi olan belge kaldırılamaz.", "warning");
    public static readonly ErrorDefinition SupplierInvalid = new("BF_SUPPLIER_INVALID", "Tedarikçi adı, türü, durumu ve işlem bilgilerini kontrol edin.", "error");
    public static readonly ErrorDefinition SupplierDuplicate = new("BF_SUPPLIER_DUPLICATE", "Bu yönetim kapsamında aynı adlı tedarikçi zaten mevcut.", "warning");
    public static readonly ErrorDefinition PlanInvalid = new("BF_PLAN_INVALID", "Planlama için 1–200 karakter başlık, geçerli tür, durum ve 2000–2100 arası tarih gerekli; açıklama en fazla 2000, konum ve sorumlu en fazla 200 karakter olabilir.", "error");
    public static readonly ErrorDefinition ParkingInvalid = new("BF_PARKING_INVALID", "Otopark alanlarını kontrol edin: plaka 2–12 harf/rakam, geçerli tür, tarih aralığı ve en fazla izin verilen uzunluklar.", "error");
    public static readonly ErrorDefinition ParkingPolicyRequired = new("BF_PARKING_POLICY_REQUIRED", "Önce bu yönetim alanı için otopark kurallarını (kota ve misafir süresi) kaydedin.", "warning");
    public static readonly ErrorDefinition ParkingQuotaExceeded = new("BF_PARKING_QUOTA_EXCEEDED", "Daire veya kişi için izin verilen araç ya da park yeri sayısı doldu.", "warning");
    public static readonly ErrorDefinition ParkingPlateTaken = new("BF_PARKING_PLATE_TAKEN", "Bu plaka bu yönetim alanında zaten kayıtlı veya onay bekliyor.", "warning");
    public static readonly ErrorDefinition ParkingSpaceOccupied = new("BF_PARKING_SPACE_OCCUPIED", "Park yeri seçilen tarih veya saat aralığında dolu ya da bu işleme uygun değil.", "warning");
    public static readonly ErrorDefinition ParkingInUse = new("BF_PARKING_IN_USE", "Bu kayıt kullanımda olduğu için silinemez; önce bağlı park yerlerini veya tahsisleri kapatın.", "warning");
    public static readonly ErrorDefinition ParkingStateInvalid = new("BF_PARKING_STATE_INVALID", "Kayıt bu işlem için uygun durumda değil; güncel bilgileri yükleyin.", "warning");
    public static readonly ErrorDefinition ParkingRelationRequired = new("BF_PARKING_RELATION_REQUIRED", "Kişinin bu bağımsız bölümle güncel bir malik, kiracı veya sakin ilişkisi bulunmalı.", "warning");
    public static readonly ErrorDefinition ParkingRequestReused = new("BF_PARKING_REQUEST_REUSED", "Aynı istek kimliği farklı bilgilerle kullanıldı; formu yenileyip tekrar gönderin.", "warning");
    public static readonly ErrorDefinition ParkingNameTaken = new("BF_PARKING_NAME_TAKEN", "Aynı ad veya park yeri kodu bu otoparkta zaten var.", "warning");
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
    public static readonly ErrorDefinition WebsiteMediaInvalid = new("BF_WEBSITE_MEDIA_INVALID", "Website media or rights declaration is invalid.", "error");
    public static readonly ErrorDefinition WebsiteMediaInUse = new("BF_WEBSITE_MEDIA_IN_USE", "Website media is referenced by a draft or publication.", "warning");
    public static readonly ErrorDefinition WebsiteSiteKeyInvalid = new("BF_WEBSITE_SITE_KEY_INVALID", "Site anahtarı 3–63 karakter; küçük harf, rakam ve tire içermeli, ayrılmış bir ad olmamalı.", "error");
    public static readonly ErrorDefinition WebsiteDetailsInvalid = new("BF_WEBSITE_DETAILS_INVALID", "Site adı, slogan ve kurumsal iletişim bilgileri düz metin ve geçerli biçimde olmalı.", "error");
    public static readonly ErrorDefinition WebsiteThemeInvalid = new("BF_WEBSITE_THEME_INVALID", "Tema rengi #RRGGBB biçiminde ve beyaz yazıyla okunabilir olmalı.", "error");
    public static readonly ErrorDefinition WebsitePageInvalid = new("BF_WEBSITE_PAGE_INVALID", "Sayfa türü, adres, başlık ve sıra bilgileri geçerli olmalı.", "error");
    public static readonly ErrorDefinition WebsiteSectionInvalid = new("BF_WEBSITE_SECTION_INVALID", "İçerik bloğu bu tür için izinli alanlarla, HTML içermeden ve güvenli bağlantılarla girilmeli.", "error");
    public static readonly ErrorDefinition WebsiteState = new("BF_WEBSITE_STATE", "Bu işlem sitenin mevcut yayın durumunda yapılamaz.", "warning");
    public static readonly ErrorDefinition WebsiteSiteExists = new("BF_WEBSITE_SITE_EXISTS", "Bu yönetim alanının zaten bir web sitesi var.", "warning");
    public static readonly ErrorDefinition WebsiteSiteKeyTaken = new("BF_WEBSITE_SITE_KEY_TAKEN", "Bu site anahtarı kullanılıyor; başka bir anahtar seçin.", "warning");
    public static readonly ErrorDefinition WebsitePageSlugTaken = new("BF_WEBSITE_PAGE_SLUG_TAKEN", "Bu sayfa adresi veya ana sayfa sitede zaten var.", "warning");
    public static readonly ErrorDefinition WebsitePageRequired = new("BF_WEBSITE_PAGE_REQUIRED", "Ana sayfa, KVKK aydınlatma metni ve çerez politikası sayfaları silinemez.", "warning");
    public static readonly ErrorDefinition WebsitePublishIncomplete = new("BF_WEBSITE_PUBLISH_INCOMPLETE", "Yayından önce ana sayfa, KVKK aydınlatma metni ve çerez politikası doldurulmalı; [DOLDURUN] işaretleri kalmamalı.", "warning");
    public static readonly ErrorDefinition WebsiteRequestConflict = new("BF_WEBSITE_REQUEST_CONFLICT", "Bu istek kimliği farklı içerikle kullanılmış.", "warning");
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
    public static readonly ErrorDefinition PersonDuplicateIdentity = new("BF_PERSON_DUPLICATE_IDENTITY", "Bu kimlik numarasıyla (T.C. kimlik, vergi veya yabancı kimlik/pasaport no) kayıtlı başka bir kişi var. Yeni kişi açmak yerine mevcut kişiyi kullanın.", "warning");
    public static readonly ErrorDefinition PersonDuplicatePhone = new("BF_PERSON_DUPLICATE_PHONE", "Bu cep telefonu başka bir kişide kayıtlı. Aynı kişiyse mevcut kaydı kullanın; değilse farklı bir numara girin.", "warning");
    public static readonly ErrorDefinition PersonDuplicateEmail = new("BF_PERSON_DUPLICATE_EMAIL", "Bu e-posta adresi başka bir kişide kayıtlı. Aynı kişiyse mevcut kaydı kullanın; değilse farklı bir adres girin.", "warning");
    public static readonly ErrorDefinition PersonLinkUnavailable = new("BF_PERSON_LINK_UNAVAILABLE", "Bu kişi için bağlama kodu oluşturulamaz: kişi etkin gerçek kişi olmalı, yetkili alanda güncel ilişkisi veya görevi bulunmalı ve bağlı hesabı olmamalı.", "warning");
    public static readonly ErrorDefinition LeaseOverlap = new("BF_LEASE_OVERLAP", "Bu bağımsız bölümde aynı dönemde başka bir kira sözleşmesi var. Ortak kiracıyı mevcut sözleşmeye taraf olarak ekleyin.", "warning");
    public static readonly ErrorDefinition LeasePartyOutside = new("BF_LEASE_PARTY_OUTSIDE", "Kiracı dönemi sözleşme dönemi içinde olmalı; bitirilecek sözleşmenin daha geç biten veya sonra başlayan tarafı olamaz.", "warning");
    public static readonly ErrorDefinition LatePenaltyInvalid = new("BF_LATE_PENALTY_INVALID", "Gecikme politikası alanlarını kontrol edin: geçerli tarih aralığı, kanuni aylık %5 oran, gün/30 yöntemi ve KMK m.20 dayanağı.", "error");
    public static readonly ErrorDefinition LatePenaltyOverlap = new("BF_LATE_PENALTY_OVERLAP", "Bu tarih aralığında onaylı başka bir gecikme politikası var.", "warning");
    public static readonly ErrorDefinition LatePenaltyApproval = new("BF_LATE_PENALTY_APPROVAL", "Kanuni orandan farklı oran için ayrı hukuki dayanak, onay belgesi ve taslağı oluşturandan farklı bir yetkilinin onayı gerekir.", "warning");
    public static readonly ErrorDefinition LatePenaltyState = new("BF_LATE_PENALTY_STATE", "Gecikme politikası bu işlem için uygun durumda değil; güncel kaydı kontrol edin.", "warning");
    public static readonly ErrorDefinition LatePenaltyCrystallized = new("BF_LATE_PENALTY_CRYSTALLIZED", "Bu tarih kesinleşmiş gecikme tahakkukunun içinde kalıyor; geçmiş tahakkuklar sessizce değiştirilemez.", "warning");
    public static readonly ErrorDefinition LatePenaltySwitchInvalid = new("BF_LATE_PENALTY_SWITCH_INVALID", "Uygulama ayarı için bugünden önce olmayan yürürlük tarihi ve gerekçe girin.", "error");
    public static readonly ErrorDefinition LatePenaltyRequestConflict = new("BF_LATE_PENALTY_REQUEST_CONFLICT", "Bu istek kimliği farklı bir gecikme işlemi için kullanılmış.", "warning");
    public static readonly ErrorDefinition LatePenaltyRebuildBlocked = new("BF_LATE_PENALTY_REBUILD_BLOCKED", "Bu tarihten sonra ödenmiş veya muaf tutulmuş gecikme tahakkuku var; önce sonraki ödemeleri iptal edin.", "warning");
    public static readonly ErrorDefinition LatePenaltyBalance = new("BF_LATE_PENALTY_BALANCE", "Gecikme tazminatı dağıtımı ödeme tarihindeki tazminat bakiyesini aşamaz.", "warning");
    public static readonly ErrorDefinition LatePenaltyWaive = new("BF_LATE_PENALTY_WAIVE", "Yalnız ödenmemiş aktif gecikme tahakkuku gerekçeyle muaf tutulabilir.", "warning");
    public static readonly ErrorDefinition LatePenaltyDate = new("BF_LATE_PENALTY_DATE", "Hesaplama tarihi geçerli olmalı ve bugünden sonra olamaz.", "error");
    public static readonly ErrorDefinition LatePenaltyDocument = new("BF_LATE_PENALTY_DOCUMENT", "Onay belgesi bu yönetim alanına ait, silinmemiş bir belge olmalı.", "error");
    public static readonly ErrorDefinition LatePenaltyChargePaid = new("BF_LATE_PENALTY_CHARGE_PAID", "Gecikme tazminatı tahsil edilmiş borç iptal edilemez; önce ilgili ödemeyi iptal edin.", "warning");
    public static readonly ErrorDefinition DuesKindInvalid = new("BF_DUES_KIND_INVALID", "Borç türünü seçin; ek ödeme için kategori zorunludur, aidatta kategori olmaz.", "error");
    public static readonly ErrorDefinition PaymentSettingsInvalid = new("BF_PAYMENT_SETTINGS_INVALID", "Ödeme ayarlarını kontrol edin: makbuz öneki 2–10 harf/rakam, komisyon politikası yalnız yönetimin üstlenmesi olabilir.", "error");
    public static readonly ErrorDefinition PaymentBankAccountInvalid = new("BF_PAYMENT_BANK_ACCOUNT_INVALID", "Banka adı, hesap sahibi, amaç, blok ve geçerlilik tarihlerini kontrol edin.", "error");
    public static readonly ErrorDefinition PaymentIbanInvalid = new("BF_PAYMENT_IBAN_INVALID", "IBAN geçersiz; TR ile başlayan 26 karakterlik ve kontrol basamağı doğru bir IBAN girin.", "error");
    public static readonly ErrorDefinition PaymentIbanDuplicate = new("BF_PAYMENT_IBAN_DUPLICATE", "Bu IBAN bu yönetim alanında zaten açık veya onay bekleyen bir hesapta kayıtlı.", "warning");
    public static readonly ErrorDefinition PaymentBankAccountState = new("BF_PAYMENT_BANK_ACCOUNT_STATE", "Banka hesabı bu işlem için uygun durumda değil; güncel kaydı kontrol edin.", "warning");
    public static readonly ErrorDefinition PaymentSecondApprover = new("BF_PAYMENT_SECOND_APPROVER", "Bu işlem çift onaylıdır; talebi oluşturan kişiden farklı bir yetkili onaylamalıdır.", "warning");
    public static readonly ErrorDefinition PaymentBankAccountUnusable = new("BF_PAYMENT_BANK_ACCOUNT_UNUSABLE", "Seçilen banka hesabı bu yönetim alanında ödeme tarihinde geçerli değil veya havale dışı bir yöntem seçildi.", "warning");
    public static readonly ErrorDefinition PaymentMerchantInvalid = new("BF_PAYMENT_MERCHANT_INVALID", "Sağlayıcı, etiket, üye işyeri numarası ve gizli anahtar referansını kontrol edin; gizli anahtarın kendisini girmeyin.", "error");
    public static readonly ErrorDefinition PaymentLiveDisabled = new("BF_PAYMENT_LIVE_DISABLED", "Canlı ödeme ortamı kapalıdır; yalnız sandbox kullanılabilir. Canlıya geçiş ayrı onay gerektirir.", "warning");
    public static readonly ErrorDefinition PaymentProviderUnavailable = new("BF_PAYMENT_PROVIDER_UNAVAILABLE", "Ödeme sağlayıcısı entegre değil veya sistem yöneticisi tarafından kapatılmış.", "warning");
    public static readonly ErrorDefinition PaymentProviderFailure = new("BF_PAYMENT_PROVIDER_FAILURE", "Ödeme sağlayıcısına şu anda ulaşılamadı; işlem yapılmadı. Daha sonra tekrar deneyin.", "warning");
    public static readonly ErrorDefinition PaymentOnlineDisabled = new("BF_PAYMENT_ONLINE_DISABLED", "Bu yönetim alanında online ödeme açık değil veya geçerli bir üye işyeri tanımlı değil.", "warning");
    public static readonly ErrorDefinition PaymentIntentInvalid = new("BF_PAYMENT_INTENT_INVALID", "Ödenecek borçları ve tutarı kontrol edin.", "error");
    public static readonly ErrorDefinition PaymentIntentActive = new("BF_PAYMENT_INTENT_ACTIVE", "Seçilen borçlardan biri için devam eden bir online ödeme var; sonucunu bekleyin.", "warning");
    public static readonly ErrorDefinition PaymentNothingDue = new("BF_PAYMENT_NOTHING_DUE", "Seçilen borçlarda ödenecek bakiye kalmadı; listeyi yenileyin.", "warning");
    public static readonly ErrorDefinition PaymentPartialDisabled = new("BF_PAYMENT_PARTIAL_DISABLED", "Bu yönetim alanında kısmi online ödeme kapalıdır; borcun tamamını ödeyin.", "warning");
    public static readonly ErrorDefinition PaymentPartialAmount = new("BF_PAYMENT_PARTIAL_AMOUNT", "Kısmi ödeme tutarı sıfırdan büyük olmalı ve seçilen borçların güncel toplamını aşmamalıdır.", "error");
    public static readonly ErrorDefinition PaymentIntentState = new("BF_PAYMENT_INTENT_STATE", "Online ödeme bu işlem için uygun durumda değil; güncel durumu yenileyin.", "warning");
    public static readonly ErrorDefinition PaymentWebhookInvalid = new("BF_PAYMENT_WEBHOOK_INVALID", "Ödeme bildirimi doğrulanamadı.", "error");
    public static readonly ErrorDefinition PaymentRefundInvalid = new("BF_PAYMENT_REFUND_INVALID", "İade için istek kimliği ve gerekçe girin; yalnız tam iade desteklenir.", "error");
    public static readonly ErrorDefinition PaymentRefundState = new("BF_PAYMENT_REFUND_STATE", "İade bu işlem için uygun durumda değil veya bu ödeme için açık bir iade talebi var.", "warning");
    public static readonly ErrorDefinition PaymentOnlineCancel = new("BF_PAYMENT_ONLINE_CANCEL", "Online tahsilat elle iptal edilemez; iade talebi oluşturun.", "warning");
    public static readonly ErrorDefinition PaymentReviewUnallocatable = new("BF_PAYMENT_REVIEW_UNALLOCATABLE", "Alınan tutar güncel borçlara tamamen dağıtılamıyor; iade talebi oluşturun veya borçları kontrol edin.", "warning");
    public static readonly ErrorDefinition PaymentReconciliationState = new("BF_PAYMENT_RECONCILIATION_STATE", "Mutabakat kaydı zaten çözülmüş veya güncel değil.", "warning");
    public static readonly ErrorDefinition QaRunActive = new("BF_QA_RUN_ACTIVE", "Bu senaryo için kuyrukta veya çalışan bir koşum var.", "warning");
    public static readonly ErrorDefinition QaRunState = new("BF_QA_RUN_STATE", "Test koşumu bu işlem için uygun durumda değil; listeyi yenileyin.", "warning");
    public static readonly ErrorDefinition QaRequestReused = new("BF_QA_REQUEST_REUSED", "Bu istek kimliği farklı bir test koşumu için kullanıldı.", "error");
    public static readonly ErrorDefinition QaRetryLimit = new("BF_QA_RETRY_LIMIT", "Bu koşum için en fazla 5 deneme yapılabilir.", "warning");
    public static readonly ErrorDefinition QaAiUnavailable = new("BF_QA_AI_UNAVAILABLE", "AkaAI test asistanına şu an ulaşılamıyor; senaryoları katalogdan elle seçebilirsiniz.", "warning");
    public static readonly ErrorDefinition QaAiPlanInvalid = new("BF_QA_AI_PLAN_INVALID", "AkaAI izin verilmeyen veya geçersiz bir plan döndürdü; plan reddedildi.", "warning");
    public static readonly ErrorDefinition QaAiBudget = new("BF_QA_AI_BUDGET", "Bugünkü AI test asistanı kullanım sınırına ulaştınız.", "warning");
    public static readonly ErrorDefinition QaAiPromptInvalid = new("BF_QA_AI_PROMPT_INVALID", "Test isteği 1-1000 karakter olmalı ve kontrol karakteri içermemeli.", "error");
    public static readonly ErrorDefinition QaDataProtected = new("BF_QA_DATA_PROTECTED", "Demo verisi korunur; temizlenemez.", "warning");
    public static readonly ErrorDefinition QaDataConfirm = new("BF_QA_DATA_CONFIRM", "Onay metni ön izlemedeki ifadeyle birebir aynı olmalı.", "error");
    public static readonly ErrorDefinition QaDataBlocked = new("BF_QA_DATA_BLOCKED", "Bu veri partisi temizlenemez: finans geçmişi veya parti dışı bağlı kayıt var.", "warning");
    public static readonly ErrorDefinition QaDataState = new("BF_QA_DATA_STATE", "Veri partisi değişmiş veya zaten temizlenmiş; ön izlemeyi yenileyin.", "warning");
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
    public static readonly ErrorDefinition MobileSyncConflict = new("BF_MOBILE_SYNC_CONFLICT", "Kayıt değişti veya işlem kimliği başka bir içerikle kullanıldı. Güncel bilgiyi kontrol edin.", "warning");
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
        SurveyInvalid, SurveyQuestionInvalid, SurveyState, SurveyAnswerInvalid, SurveyAlreadyAnswered, SurveyNotOpen,
        PersonProfileInvalid, PersonAccountInvalid, PersonMessageInvalid, PersonMessageConflict, PersonCodeUnavailable,
        SupplierInvalid, SupplierDuplicate, SupplierContractState, SupplierQuoteState, SupplierWorkOrderState, SupplierMaintenanceState, SupplierMaintenanceConflict, SupplierInvoiceDuplicate, SupplierInvoiceState, SupplierInvoiceLinked, SupplierDocumentProtected, SupplierPropertyHistory,
        MobileRegistrationInvalid, MobileRegistrationConflict, MobileRateLimited, MobileLinkRejected, MobileLinkLocked,
        UserInvalid, UserPasswordWeak, UserExists, UserRoleForbidden, UserSelfChange, UserLinkConflict, IdentityUnavailable,
        SmsInvalid, SmsPhoneInvalid, SmsTemplateParameterInvalid, SmsAccountRequired, SmsCreditInsufficient, SmsTemplateConflict, SmsPhoneNotVerified,
        ConsentInvalid, ConsentTextInvalid, ConsentLinkInvalid, ConsentTypeConflict, ConsentSystemType, ConsentTypeInUse, ConsentOperationConflict, ConsentTypeDisabled, ConsentRequired, ConsentTextOutdated, ConsentNotWithdrawable,
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
        WebsiteMediaInvalid,
        WebsiteMediaInUse,
        WebsiteDomainInvalid,
        WebsiteDomainConflict,
        WebsiteDomainUnverified,
        WebsitePreviewUnavailable,

        InquiryRejected,
        InquiryUnavailable,
        ListingInvalid,
        ListingState,
        ListingConsent,
        ListingCompliance,
        ListingVerification,

        WebsiteSiteKeyInvalid,
        WebsiteDetailsInvalid,
        WebsiteThemeInvalid,
        WebsitePageInvalid,
        WebsiteSectionInvalid,
        WebsiteState,
        WebsiteSiteExists,
        WebsiteSiteKeyTaken,
        WebsitePageSlugTaken,
        WebsitePageRequired,
        WebsitePublishIncomplete,
        WebsiteRequestConflict,
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
        ParkingInvalid,
        ParkingPolicyRequired,
        ParkingQuotaExceeded,
        ParkingPlateTaken,
        ParkingSpaceOccupied,
        ParkingInUse,
        ParkingStateInvalid,
        ParkingRelationRequired,
        ParkingRequestReused,
        ParkingNameTaken,
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
        PersonDuplicateIdentity,
        PersonDuplicatePhone,
        PersonDuplicateEmail,
        PersonLinkUnavailable,
        LeaseOverlap,
        LeasePartyOutside,
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
        LatePenaltyInvalid,
        LatePenaltyOverlap,
        LatePenaltyApproval,
        LatePenaltyState,
        LatePenaltyCrystallized,
        LatePenaltySwitchInvalid,
        LatePenaltyRequestConflict,
        LatePenaltyRebuildBlocked,
        LatePenaltyBalance,
        LatePenaltyWaive,
        LatePenaltyDate,
        LatePenaltyDocument,
        LatePenaltyChargePaid,
        DuesKindInvalid,
        PaymentSettingsInvalid,
        PaymentBankAccountInvalid,
        PaymentIbanInvalid,
        PaymentIbanDuplicate,
        PaymentBankAccountState,
        PaymentSecondApprover,
        PaymentBankAccountUnusable,
        PaymentMerchantInvalid,
        PaymentLiveDisabled,
        PaymentProviderUnavailable,
        PaymentProviderFailure,
        PaymentOnlineDisabled,
        PaymentIntentInvalid,
        PaymentIntentActive,
        PaymentNothingDue,
        PaymentPartialDisabled,
        PaymentPartialAmount,
        PaymentIntentState,
        PaymentWebhookInvalid,
        PaymentRefundInvalid,
        PaymentRefundState,
        PaymentOnlineCancel,
        PaymentReviewUnallocatable,
        PaymentReconciliationState,
        QaRunActive,
        QaRunState,
        QaRequestReused,
        QaRetryLimit,
        QaAiUnavailable,
        QaAiPlanInvalid,
        QaAiBudget,
        QaAiPromptInvalid,
        QaDataProtected,
        QaDataConfirm,
        QaDataBlocked,
        QaDataState,
        MobileSyncConflict,
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
