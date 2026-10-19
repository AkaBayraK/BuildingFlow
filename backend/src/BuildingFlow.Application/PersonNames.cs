using System.Globalization;
namespace BuildingFlow.Application;
// User decision 17.10.2026: a person's surname is stored in capitals and each given name with only its first letter capitalised.
// Turkish rules (i/İ, ı/I) apply unless the person is a foreign citizen; company titles (LEGAL) are kept as written.
public static class PersonNames {
 private static readonly CultureInfo Turkish=CultureInfo.GetCultureInfo("tr-TR");
 private static CultureInfo Culture(string? citizenship)=>citizenship=="FOREIGN"?CultureInfo.InvariantCulture:Turkish;
 private static string Collapse(string value)=>string.Join(' ',value.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries));
 public static string? Given(string? value,string? citizenship=null){
  if(value==null)return null;var culture=Culture(citizenship);var chars=Collapse(value).ToLower(culture).ToCharArray();
  // A new word starts after a space or a hyphen (Ayşe-Nur); the rest of the word stays lower case.
  for(var i=0;i<chars.Length;i++)if(char.IsLetter(chars[i])&&(i==0||chars[i-1] is ' ' or '-'))chars[i]=culture.TextInfo.ToUpper(chars[i]);
  return new string(chars);
 }
 public static string? Family(string? value,string? citizenship=null)=>value==null?null:Collapse(value).ToUpper(Culture(citizenship));
 public static (string? First,string? Last) Apply(string? first,string? last,string? kind,string? citizenship)=>kind=="LEGAL"?(first?.Trim(),last?.Trim()):(Given(first,citizenship),Family(last,citizenship));
}
