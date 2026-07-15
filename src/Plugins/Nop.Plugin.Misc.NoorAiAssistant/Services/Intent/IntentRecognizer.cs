using System.Text.RegularExpressions;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Lighting;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Recommendation;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Text;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Intent;

/// <summary>
/// A rules engine, not a model. It reads a message top-down through the intents in order of how specific
/// they are, and the first one that fits wins.
/// </summary>
/// <remarks>
/// Order is the whole design. "Compare the 9W and the 12W spotlight for my 5x4 living room" contains a
/// fixture, a room and dimensions, and would satisfy the recommendation, quantity and lighting rules too —
/// it is a comparison because comparison is checked first. Reordering these checks changes behaviour, so
/// each one is placed with a note on why it sits where it does.
/// </remarks>
public partial class IntentRecognizer : IIntentRecognizer
{
    #region Fields

    private readonly ILightingCalculator _lightingCalculator;

    private static readonly string[] _comparisonTriggers =
        ["compare", "comparison", "vs", "versus", "difference between", "which is better", "better between",
         "قارن", "مقارنه", "الفرق بين", "ايهما افضل", "ايهما احسن", "افضل بين", "الفرق"];

    /// <summary>
    /// Splits "compare A and B" into A and B. Ordered longest-first so " versus " is tried before " vs ".
    /// </summary>
    private static readonly string[] _comparisonSeparators =
        [" versus ", " vs. ", " vs ", " and ", " or ", " with ", " مع ", " و بين ", " وبين ", " ام ", " او ", " و "];

    private static readonly string[] _quantityTriggers =
        ["how many", "how much", "how many do i need", "number of", "quantity",
         "كم عدد", "كم احتاج", "كم اريد", "كم يلزم", "كم قطعه", "كم متر", "العدد", "كميه"];

    private static readonly string[] _greetings =
        ["hi", "hey", "hello", "good morning", "good evening", "good afternoon",
         "مرحبا", "السلام عليكم", "اهلا", "هلا", "صباح الخير", "مساء الخير"];

    private static readonly string[] _smallTalk =
        ["thanks", "thank you", "thx", "ok", "okay", "great", "perfect", "bye", "goodbye",
         "شكرا", "مشكور", "تمام", "ممتاز", "الله يعطيك العافيه", "مع السلامه", "جزاك الله"];

    /// <summary>
    /// Phrases that mean "show me products of this kind" rather than merely mentioning the kind.
    /// </summary>
    private static readonly string[] _recommendationTriggers =
        ["i need", "i want", "looking for", "recommend", "suggest", "show me", "do you have", "i am looking",
         "احتاج", "اريد", "ابغي", "ابي", "عايز", "عاوز", "بدي", "ابحث", "اقترح", "رشح", "عندكم", "عندك", "ورني"];

    private static readonly Regex _comparisonPattern = ComparisonPatternRegex();

    #endregion

    #region Ctor

    public IntentRecognizer(ILightingCalculator lightingCalculator)
    {
        _lightingCalculator = lightingCalculator;
    }

    #endregion

    #region Utilities

    private static bool ContainsAny(string normalized, string[] terms)
    {
        return terms.Any(term => TextNormalizer.ContainsTerm(normalized, term));
    }

    /// <summary>
    /// Determines whether the message is *only* a greeting. "hi" is a greeting; "hi, I need a chandelier"
    /// is a product request that happens to open politely, and answering it with "hello!" would be useless.
    /// </summary>
    private static bool IsBareGreeting(string normalized, string[] terms)
    {
        if (!ContainsAny(normalized, terms))
            return false;

        //strip the matched phrase and see whether anything of substance is left
        var remainder = normalized;
        foreach (var term in terms.OrderByDescending(t => t.Length))
            remainder = remainder.Replace(TextNormalizer.Normalize(term), " ", StringComparison.Ordinal);

        return TextNormalizer.ExtractKeywords(remainder).Count == 0;
    }

    /// <summary>
    /// Pulls the two operands out of a comparison. Returns false when the shopper said "compare" but named
    /// only one thing ("compare the chandeliers"), which is a recommendation, not a comparison.
    /// </summary>
    private static bool TryExtractComparisonTerms(string normalized, out string termA, out string termB)
    {
        termA = termB = null;

        //drop the trigger verb and any lead-in, so "compare between X and Y" leaves "X and Y"
        var body = _comparisonPattern.Replace(normalized, " ").Trim();

        foreach (var separator in _comparisonSeparators)
        {
            var index = body.IndexOf(separator, StringComparison.Ordinal);
            if (index <= 0)
                continue;

            var left = body[..index].Trim();
            var right = body[(index + separator.Length)..].Trim();

            //both sides must carry a real term, or this was a stray "and"
            if (TextNormalizer.ExtractKeywords(left).Count == 0 || TextNormalizer.ExtractKeywords(right).Count == 0)
                continue;

            termA = left;
            termB = right;

            return true;
        }

        return false;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Reads a message
    /// </summary>
    public virtual IntentAnalysis Analyze(string message)
    {
        var normalized = TextNormalizer.Normalize(message);

        var analysis = new IntentAnalysis
        {
            NormalizedMessage = normalized,
            IsArabic = TextNormalizer.IsArabic(message),
            Keywords = TextNormalizer.ExtractKeywords(message),
            Need = NeedCatalog.Match(normalized),
            Room = RoomCatalog.Match(normalized),
            Dimensions = _lightingCalculator.ParseDimensions(normalized)
        };

        if (normalized.Length == 0)
        {
            analysis.Intent = ChatIntent.Unknown;

            return analysis;
        }

        //1. comparison first: a comparison message legitimately mentions fixtures, rooms and sizes, so every
        //later rule would also fire on it
        if (ContainsAny(normalized, _comparisonTriggers) &&
            TryExtractComparisonTerms(normalized, out var termA, out var termB))
        {
            analysis.Intent = ChatIntent.ProductComparison;
            analysis.ComparisonTermA = termA;
            analysis.ComparisonTermB = termB;

            return analysis;
        }

        //2. explicit dimensions are unambiguous — nobody types "5 x 4" for any reason other than a room
        if (analysis.Dimensions is not null)
        {
            analysis.Intent = ChatIntent.LightingPlan;

            return analysis;
        }

        //3. "how many spotlights do I need" — a count, before it can be read as a request for products
        if (ContainsAny(normalized, _quantityTriggers))
        {
            analysis.Intent = ChatIntent.QuantityEstimation;

            return analysis;
        }

        //4. a bare greeting, checked only after the substantive rules so "hi, I need a chandelier" is not
        //answered with "hello"
        if (IsBareGreeting(normalized, _greetings))
        {
            analysis.Intent = ChatIntent.Greeting;

            return analysis;
        }

        if (IsBareGreeting(normalized, _smallTalk))
        {
            analysis.Intent = ChatIntent.SmallTalk;

            return analysis;
        }

        //5. a known product kind: a recommendation when the shopper asked for it ("I need a chandelier"),
        //and also on a bare "chandelier", which is how people actually search
        if (analysis.Need is not null)
        {
            analysis.Intent = ChatIntent.ProductRecommendation;

            return analysis;
        }

        //6. anything else with substance is a free-text catalogue search; the provider decides what to say
        //when it finds nothing
        analysis.Intent = analysis.Keywords.Count > 0 ? ChatIntent.ProductSearch : ChatIntent.Unknown;

        return analysis;
    }

    /// <summary>
    /// Determines whether the message asks for products of a known kind, rather than merely naming one.
    /// Exposed so the provider can phrase a recommendation as an answer to a request ("Here are our
    /// chandeliers") rather than as an unprompted suggestion.
    /// </summary>
    public static bool IsExplicitRequest(string normalizedMessage)
    {
        return ContainsAny(normalizedMessage, _recommendationTriggers);
    }

    #endregion

    #region Generated regexes

    [GeneratedRegex(@"\b(compare|comparison|versus|difference between|which is better|better between|between)\b|قارن|مقارنه|الفرق بين|ايهما افضل|ايهما احسن|افضل بين|بين",
        RegexOptions.Compiled | RegexOptions.IgnoreCase, 200)]
    private static partial Regex ComparisonPatternRegex();

    #endregion
}
