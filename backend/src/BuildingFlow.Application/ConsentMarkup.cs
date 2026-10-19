using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
namespace BuildingFlow.Application;

// Consent texts use a small project-owned format instead of HTML, so no sanitizer package is needed and no client
// ever renders raw markup. Web and mobile carry identical parsers; docs/api/consent-markup-vectors.json pins all three.
//   # / ## / ### heading, blank line = new paragraph, "- " bullet, "1. " numbered item,
//   **bold**, [text](https://…) or [text](mailto:…). Anything else is plain text.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MarkupText), "text")]
[JsonDerivedType(typeof(MarkupBold), "bold")]
[JsonDerivedType(typeof(MarkupLink), "link")]
public abstract record MarkupInline;
public sealed record MarkupText(string Text) : MarkupInline;
public sealed record MarkupBold(string Text) : MarkupInline;
public sealed record MarkupLink(string Text, string Href) : MarkupInline;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MarkupHeading), "heading")]
[JsonDerivedType(typeof(MarkupParagraph), "paragraph")]
[JsonDerivedType(typeof(MarkupList), "list")]
public abstract record MarkupBlock;
public sealed record MarkupHeading(int Level, IReadOnlyList<MarkupInline> Content) : MarkupBlock;
public sealed record MarkupParagraph(IReadOnlyList<MarkupInline> Content) : MarkupBlock;
public sealed record MarkupList(bool Ordered, int Start, IReadOnlyList<IReadOnlyList<MarkupInline>> Items) : MarkupBlock;

public sealed record MarkupResult(IReadOnlyList<MarkupBlock> Blocks, string? Error);

public static partial class ConsentMarkup
{
    public const int MaxLength = 50_000;
    public const int MaxLines = 2_000;
    public const int MaxHrefLength = 2_000;
    public const string ErrorEmpty = "EMPTY", ErrorTooLong = "TOO_LONG", ErrorTooManyLines = "TOO_MANY_LINES", ErrorControl = "CONTROL_CHAR", ErrorLink = "LINK";

    [GeneratedRegex(@"^(#{1,3}) (.*)$", RegexOptions.CultureInvariant)] private static partial Regex HeadingLine();
    [GeneratedRegex(@"^- (.*)$", RegexOptions.CultureInvariant)] private static partial Regex BulletLine();
    [GeneratedRegex(@"^([0-9]{1,3})\. (.*)$", RegexOptions.CultureInvariant)] private static partial Regex OrderedLine();
    [GeneratedRegex(@"^[^\s@<>()\[\]]+@[^\s@<>()\[\]]+\.[^\s@<>()\[\]]+$", RegexOptions.CultureInvariant)] private static partial Regex Email();

    // Line endings become \n and tabs become spaces; the stored body is this normalized form.
    public static string Normalize(string body) => body.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Replace('\t', ' ');

    public static MarkupResult Parse(string? body)
    {
        if (body is null) return new([], ErrorEmpty);
        body = Normalize(body);
        if (body.Length > MaxLength) return new([], ErrorTooLong);
        if (HasHiddenCharacters(body)) return new([], ErrorControl);
        var lines = body.Split('\n');
        if (lines.Length > MaxLines) return new([], ErrorTooManyLines);

        var blocks = new List<MarkupBlock>();
        var paragraph = new List<string>();
        List<IReadOnlyList<MarkupInline>>? items = null; var ordered = false; var start = 1;
        string? error = null;

        void FlushParagraph()
        {
            if (paragraph.Count == 0) return;
            var content = Inline(string.Join('\n', paragraph), ref error);
            if (content.Count > 0) blocks.Add(new MarkupParagraph(content));
            paragraph.Clear();
        }
        void FlushList()
        {
            if (items is null) return;
            blocks.Add(new MarkupList(ordered, start, items.ToArray()));
            items = null;
        }

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (line.Trim().Length == 0) { FlushParagraph(); FlushList(); continue; }
            var heading = HeadingLine().Match(line);
            if (heading.Success && heading.Groups[2].Value.Trim().Length > 0)
            {
                FlushParagraph(); FlushList();
                blocks.Add(new MarkupHeading(heading.Groups[1].Length, Inline(heading.Groups[2].Value.Trim(), ref error)));
                continue;
            }
            var bullet = BulletLine().Match(line);
            var number = bullet.Success ? Match.Empty : OrderedLine().Match(line);
            var itemText = bullet.Success ? bullet.Groups[1].Value : number.Success ? number.Groups[2].Value : null;
            if (itemText is not null && itemText.Trim().Length > 0)
            {
                FlushParagraph();
                var isOrdered = number.Success;
                if (items is not null && ordered != isOrdered) FlushList();
                if (items is null) { items = []; ordered = isOrdered; start = isOrdered ? int.Parse(number.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 1; }
                items.Add(Inline(itemText.Trim(), ref error));
                continue;
            }
            FlushList();
            paragraph.Add(line.Trim());
        }
        FlushParagraph(); FlushList();
        if (error is not null) return new([], error);
        return blocks.Count == 0 ? new([], ErrorEmpty) : new(blocks, null);
    }

    private static List<MarkupInline> Inline(string text, ref string? error)
    {
        var result = new List<MarkupInline>();
        var plain = new StringBuilder();
        void Push(MarkupInline inline)
        {
            if (plain.Length > 0) { result.Add(new MarkupText(plain.ToString())); plain.Clear(); }
            result.Add(inline);
        }
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var close = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (close > i + 2)
                {
                    var inner = text[(i + 2)..close];
                    if (inner.Trim().Length > 0) { Push(new MarkupBold(inner)); i = close + 2; continue; }
                }
            }
            if (text[i] == '[')
            {
                var closeText = text.IndexOf(']', i + 1);
                if (closeText > i + 1 && closeText + 1 < text.Length && text[closeText + 1] == '(' && text.IndexOf('[', i + 1, closeText - i - 1) < 0)
                {
                    var closeHref = text.IndexOf(')', closeText + 2);
                    var label = text[(i + 1)..closeText];
                    if (closeHref > closeText + 2 && !label.Contains('\n'))
                    {
                        var href = text[(closeText + 2)..closeHref];
                        if (!href.Any(char.IsWhiteSpace))
                        {
                            if (!SafeHref(href)) error ??= ErrorLink;
                            else if (label.Trim().Length > 0) { Push(new MarkupLink(label, href)); i = closeHref + 1; continue; }
                        }
                    }
                }
            }
            plain.Append(text[i]); i++;
        }
        if (plain.Length > 0) result.Add(new MarkupText(plain.ToString()));
        return result;
    }

    // Control, invisible format (bidi overrides, BOM) and broken surrogate characters could make the approved text differ
    // from what was displayed. Zero-width joiners stay allowed for scripts that need them.
    private static bool HasHiddenCharacters(string body)
    {
        for (var i = 0; i < body.Length; i++)
        {
            var c = body[i];
            System.Globalization.UnicodeCategory category;
            if (char.IsHighSurrogate(c) && i + 1 < body.Length && char.IsLowSurrogate(body[i + 1])) { category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(body, i); i++; }
            else if (char.IsSurrogate(c)) return true;
            else category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == System.Globalization.UnicodeCategory.Control && c != '\n') return true;
            if (category == System.Globalization.UnicodeCategory.Format && c is not ('\u200c' or '\u200d')) return true;
        }
        return false;
    }

    public static bool SafeHref(string href)
    {
        if (href.Length is 0 or > MaxHrefLength) return false;
        if (href.StartsWith("mailto:", StringComparison.Ordinal)) return Email().IsMatch(href[7..]);
        return href.StartsWith("https://", StringComparison.Ordinal) && Uri.TryCreate(href, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps && uri.Host.Length > 0 && string.IsNullOrEmpty(uri.UserInfo);
    }
}
