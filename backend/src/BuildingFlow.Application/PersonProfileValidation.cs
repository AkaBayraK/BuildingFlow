namespace BuildingFlow.Application;
// Both contracts use the same identity validation. Only v1 requires contact fields.
public static class PersonProfileValidation {
 public static void Validate(string? first,string? last,string? phone,string? email,string? kind,string? citizenship,string? country,string? number,DateOnly? birth,string? gender,string? address,string? linkedUserId,TimeProvider clock,bool requireContacts=true) {
        Dictionary<string, ApiErrorMessage[]> errors = [];
        void Invalid(string field) => errors[field] = [ErrorCatalog.PersonProfileInvalid.Create()];
        if (string.IsNullOrWhiteSpace(first) || first.Trim().Length > 100) Invalid("firstName");
        if (kind != "LEGAL" && string.IsNullOrWhiteSpace(last) || last?.Trim().Length > 100) Invalid("lastName");
        if ((requireContacts || !string.IsNullOrWhiteSpace(phone)) && (string.IsNullOrWhiteSpace(phone) || phone.Trim().Length > 30 || phone.Trim().Count(char.IsAsciiDigit) is < 8 or > 15 || !System.Text.RegularExpressions.Regex.IsMatch(phone.Trim(),@"^\+?[0-9 ()-]+$"))) Invalid("phoneNumber");
        if ((requireContacts || !string.IsNullOrWhiteSpace(email)) && (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 254 || !System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var parsed) || parsed.Address != email.Trim())) Invalid("email");
        if (kind is not ("NATURAL" or "LEGAL")) Invalid("personKind");
        if (citizenship is not ("DOMESTIC" or "FOREIGN")) Invalid("citizenship");
        if (string.IsNullOrWhiteSpace(country) || country.Length != 2 || country.Any(c => c is < 'A' or > 'Z') || (citizenship == "DOMESTIC" ? country != "TR" : country == "TR")) Invalid("countryCode");
        else { try { _ = new System.Globalization.RegionInfo(country); } catch (ArgumentException) { Invalid("countryCode"); } }
        if (string.IsNullOrWhiteSpace(number) || number.Trim().Length > 50 || number.Any(char.IsControl)) Invalid("identityNumber");
        else if (citizenship == "DOMESTIC" && (number.Trim().Length != (kind == "LEGAL" ? 10 : 11) || number.Trim().Any(c => c is < '0' or > '9') || (kind == "NATURAL" && number.Trim()[0] == '0'))) Invalid("identityNumber");
        else if (citizenship == "FOREIGN" && kind == "NATURAL" && (number.Trim().Length is < 6 or > 30 || number.Trim().Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))) Invalid("identityNumber");
        if (birth is { } date && (date < new DateOnly(1900,1,1) || date > DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(),TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime)) || kind == "LEGAL" && birth != null) Invalid("birthDate");
        if (gender is not (null or "FEMALE" or "MALE" or "OTHER" or "UNSPECIFIED") || kind == "LEGAL" && gender != null) Invalid("gender");
        if (address?.Trim().Length > 500 || address?.Any(c=>char.IsControl(c) && c is not ('\r' or '\n' or '\t')) == true) Invalid("address");
        if (linkedUserId?.Trim().Length > 200 || linkedUserId?.Any(char.IsControl) == true) Invalid("linkedUserId");
        if (errors.Count > 0) throw new RequestValidationException(errors);

 }
}
