using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Text;

/// <summary>
/// Turns whatever the shopper typed into something matchable.
/// </summary>
/// <remarks>
/// Arabic is the reason this class exists. The same word is routinely written several ways — إنارة / انارة,
/// ثريا / ثرية, with or without diacritics, with tatweel padding (كشـــاف) — and a naive Contains() matches
/// none of them against a keyword list. Everything here runs on both the shopper's message and on the
/// stored keywords before they are compared, so both sides land in the same normal form.
/// </remarks>
public static partial class TextNormalizer
{
    #region Fields

    /// <summary>
    /// Arabic diacritics (fatha, damma, kasra, shadda, sukun, tanween ...) and the tatweel padding character.
    /// </summary>
    private static readonly Regex _arabicDiacritics = ArabicDiacriticsRegex();

    private static readonly Regex _htmlTag = HtmlTagRegex();

    private static readonly Regex _whitespace = WhitespaceRegex();

    private static readonly Regex _nonWord = NonWordRegex();

    /// <summary>
    /// Words that carry no intent. Dropping them stops the "most searched keywords" report from being a
    /// list of "the", "a", "من" and keeps keyword matching honest.
    /// </summary>
    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        //english
        "a", "an", "the", "i", "me", "my", "we", "you", "your", "is", "are", "am", "was", "were", "be",
        "do", "does", "did", "to", "for", "of", "in", "on", "at", "by", "with", "from", "and", "or",
        "but", "if", "want", "need", "looking", "look", "please", "can", "could", "would", "will",
        "have", "has", "get", "got", "show", "me", "some", "any", "what", "which", "how", "there",
        "it", "its", "this", "that", "these", "those", "hi", "hello", "thanks", "thank",

        //arabic (already normalised: alef forms folded to ا, ة to ه, ى to ي)
        "انا", "احتاج", "اريد", "ابغي", "ابي", "عايز", "عاوز", "بدي", "لو", "سمحت", "من", "في", "على",
        "الي", "هل", "ما", "ماذا", "كيف", "كم", "عندكم", "عندك", "فيه", "بكم", "و", "او", "ال", "هذا",
        "هذه", "ذلك", "مع", "عن", "الى", "به", "لي", "شكرا", "مرحبا", "السلام", "عليكم", "ممكن", "يا"
    };

    #endregion

    #region Methods

    /// <summary>
    /// Folds a string into its matchable form: lower-cased, HTML-stripped, Arabic-normalised, whitespace
    /// collapsed. Both shopper messages and stored keyword lists go through this before comparison.
    /// </summary>
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = StripHtml(text);

        //strip diacritics and tatweel before folding letters, or the combining marks survive as separate chars
        text = _arabicDiacritics.Replace(text, string.Empty);

        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            //drop combining marks left over from FormD (mostly latin accents: é -> e)
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(c switch
            {
                //fold every alef form to a bare alef: أ إ آ ٱ -> ا
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                //taa marbuta is written interchangeably with haa: ة -> ه
                'ة' => 'ه',
                //alef maqsura is written interchangeably with yaa: ى -> ي
                'ى' => 'ي',
                //persian yeh/kaf leak in from some keyboards
                'ی' => 'ي',
                'ک' => 'ك',
                //arabic-indic digits -> latin, so "٥ في ٤" parses like "5 x 4"
                >= '٠' and <= '٩' => (char)(c - '٠' + '0'),
                >= '۰' and <= '۹' => (char)(c - '۰' + '0'),
                _ => c
            });
        }

        return _whitespace.Replace(builder.ToString().Normalize(NormalizationForm.FormC), " ")
            .Trim()
            .ToLowerInvariant();
    }

    /// <summary>
    /// Removes markup and control characters. Shopper messages are stored and echoed back, so they are
    /// stripped on the way in; the browser additionally escapes on the way out.
    /// </summary>
    public static string StripHtml(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = _htmlTag.Replace(text, " ");

        //&lt;script&gt; style entities would otherwise survive the tag strip
        text = text.Replace("&lt;", " ").Replace("&gt;", " ").Replace("&#", " ");

        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (!char.IsControl(c) || c == '\n' || c == '\t')
                builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Splits a normalised message into meaningful terms, dropping stop words and single characters.
    /// </summary>
    public static IList<string> ExtractKeywords(string text, int max = 10)
    {
        var normalized = Normalize(text);
        if (normalized.Length == 0)
            return [];

        return _nonWord.Split(normalized)
            .Where(token => token.Length > 1 && !_stopWords.Contains(token))
            .Distinct(StringComparer.Ordinal)
            .Take(max)
            .ToList();
    }

    /// <summary>
    /// Determines whether a normalised message contains a term, matching on word boundaries so "led" does
    /// not fire on "ledger". Arabic has no case and few boundaries, so a substring match is used for terms
    /// that contain Arabic letters.
    /// </summary>
    public static bool ContainsTerm(string normalizedText, string term)
    {
        if (string.IsNullOrEmpty(normalizedText) || string.IsNullOrEmpty(term))
            return false;

        var normalizedTerm = Normalize(term);
        if (normalizedTerm.Length == 0)
            return false;

        //arabic words take prefixes (ال, و, ب, ل) that would break a \b boundary match, so match loosely
        if (IsArabic(normalizedTerm))
            return normalizedText.Contains(normalizedTerm, StringComparison.Ordinal);

        //multi-word latin terms ("led strip") are a phrase; single words get boundaries
        var pattern = $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(normalizedTerm)}(?![\p{{L}}\p{{N}}])";

        return Regex.IsMatch(normalizedText, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200));
    }

    /// <summary>
    /// Expands a normalised Arabic term back into the spellings the catalogue might actually store, so a
    /// folded keyword still matches un-normalised product text under a raw SQL LIKE.
    /// </summary>
    /// <remarks>
    /// Normalisation is lossy on purpose — ة folds to ه, ى to ي, and أ إ آ ٱ to ا — but the stored product
    /// name is not normalised, so searching the folded form "نجفه" misses a product actually called "نجفة".
    /// This reverses the fold on the two positions where the ambiguity almost always sits: an initial alef
    /// (which may carry a hamza) and a final ه / ي (which may be a taa marbuta or an alef maqsura). That
    /// keeps the variant count to at most eight while covering the cases that occur in real catalogues. The
    /// original folded form is always returned first, and a non-Arabic term is returned unchanged.
    /// </remarks>
    public static IReadOnlyList<string> ArabicSearchVariants(string normalizedTerm)
    {
        if (string.IsNullOrEmpty(normalizedTerm) || !IsArabic(normalizedTerm))
            return [normalizedTerm];

        //an initial bare alef could have been written with any hamza form; the first entry is the bare alef,
        //so the untouched term is always tried first
        IEnumerable<string> heads = normalizedTerm[0] == 'ا'
            ? new[] { 'ا', 'أ', 'إ', 'آ' }.Select(alef => alef + normalizedTerm[1..])
            : [normalizedTerm];

        var variants = new List<string>();
        foreach (var head in heads)
        {
            variants.Add(head);

            //a final ه may be a taa marbuta, a final ي an alef maqsura
            variants.Add(head[^1] switch
            {
                'ه' => head[..^1] + 'ة',
                'ي' => head[..^1] + 'ى',
                _ => head
            });
        }

        return variants.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Determines whether a string contains Arabic letters.
    /// </summary>
    public static bool IsArabic(string text)
    {
        return !string.IsNullOrEmpty(text) && text.Any(c => c is >= '؀' and <= 'ۿ');
    }

    /// <summary>
    /// Determines whether a string contains Latin letters.
    /// </summary>
    /// <remarks>
    /// Used to tell "this shopper is writing English" apart from "this message has no letters at all"
    /// (e.g. "5 x 4"). The distinction matters: a message with no letters carries no clue about which
    /// language to answer in, whereas one with Latin letters plainly does — even when every word in it is
    /// a stop word, as in "hello".
    /// </remarks>
    public static bool HasLatinLetters(string text)
    {
        return !string.IsNullOrEmpty(text) && text.Any(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z');
    }

    /// <summary>
    /// Shortens a message into a conversation title.
    /// </summary>
    public static string ToTitle(string text, int maxLength = 60)
    {
        var clean = _whitespace.Replace(StripHtml(text ?? string.Empty), " ").Trim();
        if (clean.Length == 0)
            return string.Empty;

        return clean.Length <= maxLength ? clean : string.Concat(clean.AsSpan(0, maxLength - 1).TrimEnd(), "…");
    }

    #endregion

    #region Generated regexes

    [GeneratedRegex(@"[ً-ٰٟـ]", RegexOptions.Compiled)]
    private static partial Regex ArabicDiacriticsRegex();

    [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled, 200)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.Compiled)]
    private static partial Regex NonWordRegex();

    #endregion
}
