using System.Text;
using static System.FormattableString;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Models;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Catalog;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Faq;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Intent;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Lighting;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Recommendation;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Text;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Providers;

/// <summary>
/// Answers questions with no AI service behind it, using the store's own catalogue, FAQ and a lighting
/// calculator.
/// </summary>
/// <remarks>
/// This class is the whole "AI" today, and it is the whole thing that gets replaced tomorrow. It contains
/// no persistence, no HTTP, no rate limiting and no analytics — only the decision of what to say. An
/// OpenAI/Claude/Gemini/Ollama provider replaces this one class and reuses every service it calls
/// (search, recommendation, comparison, lighting, FAQ) as its function-call tools.
///
/// It is not pretending to be a language model: it recognises intent, calls the right tool, and composes
/// the result into markdown. Where it does not understand, it says so and offers what it can do, rather
/// than guessing — a wrong confident answer about how many lights a room needs costs a customer real money.
/// </remarks>
public partial class DummyChatProvider : IChatProvider
{
    #region Fields

    private readonly IChatFaqService _chatFaqService;
    private readonly IIntentRecognizer _intentRecognizer;
    private readonly ILightingCalculator _lightingCalculator;
    private readonly IProductComparisonService _productComparisonService;
    private readonly IProductSearchService _productSearchService;
    private readonly IRecommendationService _recommendationService;
    private readonly NoorAiAssistantSettings _settings;

    #endregion

    #region Ctor

    public DummyChatProvider(IChatFaqService chatFaqService,
        IIntentRecognizer intentRecognizer,
        ILightingCalculator lightingCalculator,
        IProductComparisonService productComparisonService,
        IProductSearchService productSearchService,
        IRecommendationService recommendationService,
        NoorAiAssistantSettings settings)
    {
        _chatFaqService = chatFaqService;
        _intentRecognizer = intentRecognizer;
        _lightingCalculator = lightingCalculator;
        _productComparisonService = productComparisonService;
        _productSearchService = productSearchService;
        _recommendationService = recommendationService;
        _settings = settings;
    }

    #endregion

    #region Properties

    public string SystemName => "Dummy";

    public string FriendlyName => "Built-in (no external AI service)";

    #endregion

    #region Utilities

    /// <summary>
    /// Picks the language to answer in. The shopper's own language wins over the storefront's: someone who
    /// types Arabic into the English store gets Arabic back, and vice versa.
    /// </summary>
    /// <remarks>
    /// The script the shopper typed in is the signal, not the words. Keyword extraction is no use here,
    /// because a bare greeting yields none — "hello" is a stop word, so keying off the keyword count would
    /// answer every English "hello" in Arabic on an Arabic storefront. Only when the message contains no
    /// letters at all ("5 x 4") is there genuinely nothing to go on, and the storefront decides.
    /// </remarks>
    private static bool UseArabic(IntentAnalysis analysis, ChatProviderRequest request)
    {
        if (analysis.IsArabic)
            return true;

        if (TextNormalizer.HasLatinLetters(analysis.NormalizedMessage))
            return false;

        return request.IsRightToLeft;
    }

    /// <summary>
    /// Chooses between an English and an Arabic string.
    /// </summary>
    private static string L(bool arabic, string english, string arabicText)
    {
        return arabic ? arabicText : english;
    }

    /// <summary>
    /// The chips offered under an answer, to keep a shopper who does not know what to ask next moving.
    /// </summary>
    private static IList<string> FollowUps(bool arabic, params string[] extra)
    {
        var chips = new List<string>(extra);

        chips.Add(L(arabic, "My room is 5 x 4 — what do I need?", "غرفتي 5 × 4 — ماذا أحتاج؟"));
        chips.Add(L(arabic, "What is your delivery time?", "كم مدة التوصيل؟"));

        return chips.Take(4).ToList();
    }

    #endregion

    #region Answer composition

    /// <summary>
    /// Answers a comparison: "compare A and B".
    /// </summary>
    protected virtual async Task<ChatProviderResponse> AnswerComparisonAsync(IntentAnalysis analysis, ChatProviderRequest request, bool arabic)
    {
        var productA = await _productSearchService.FindBestMatchAsync(analysis.ComparisonTermA, request.StoreId, request.LanguageId);
        var productB = await _productSearchService.FindBestMatchAsync(analysis.ComparisonTermB, request.StoreId, request.LanguageId);

        //name the side that could not be found, rather than a generic "not found" — the shopper can then
        //correct just that half
        if (productA is null || productB is null)
        {
            var missing = productA is null ? analysis.ComparisonTermA : analysis.ComparisonTermB;

            var notFound = ChatProviderResponse.FromText(
                L(arabic,
                    $"I couldn't find a product matching **{missing}**. Try the exact product name or its SKU, and I'll compare them for you.",
                    $"لم أجد منتجًا يطابق **{missing}**. جرّب كتابة اسم المنتج بالضبط أو رقم الـ SKU وسأقارن لك."),
                ChatIntent.ProductComparison);

            //offer whichever half we did find, so the turn is not a dead end
            var found = productA ?? productB;
            if (found is not null)
                notFound.Payload.Products = await _productSearchService.PrepareCardsAsync([found]);

            return notFound;
        }

        if (productA.Id == productB.Id)
        {
            return ChatProviderResponse.FromText(
                L(arabic,
                    "Those are the same product. Give me two different names and I'll compare them.",
                    "هذان نفس المنتج. أعطني اسمين مختلفين وسأقارن بينهما."),
                ChatIntent.ProductComparison);
        }

        var comparison = await _productComparisonService.CompareAsync(productA, productB, arabic);

        var response = new ChatProviderResponse
        {
            Intent = ChatIntent.ProductComparison,
            Keywords = analysis.Keywords,
            Text = L(arabic,
                $"Here's how **{comparison.ProductA.Name}** and **{comparison.ProductB.Name}** compare:",
                $"إليك المقارنة بين **{comparison.ProductA.Name}** و**{comparison.ProductB.Name}**:")
        };

        response.Payload.Comparison = comparison;
        response.Payload.FollowUps = FollowUps(arabic,
            L(arabic, "Which one is better for a bedroom?", "أيهما أفضل لغرفة النوم؟"));

        return response;
    }

    /// <summary>
    /// Answers a room: "my room is 5 x 4".
    /// </summary>
    protected virtual async Task<ChatProviderResponse> AnswerLightingPlanAsync(IntentAnalysis analysis, ChatProviderRequest request, bool arabic)
    {
        var room = analysis.Room ?? RoomCatalog.Default;
        var plan = _lightingCalculator.CalculatePlan(analysis.Dimensions, room, analysis.Need);

        var fixture = analysis.Need ?? NeedCatalog.FindBySlug("spotlight");
        plan.ColorTemperatureLabel = LightingCalculator.DescribeKelvin(plan.ColorTemperatureKelvin, arabic);
        plan.RoomType = room.GetName(arabic);
        plan.RecommendedCategory = fixture.GetName(arabic);

        var text = new StringBuilder();

        //Invariant(...) throughout: the thread culture follows the *storefront* language, so an English
        //answer in an Arabic store would otherwise come out as "3٬000 lumens" — Arabic separators inside an
        //English sentence. Prices are the exception and stay culture-formatted, because nopCommerce formats
        //those against the customer's currency and they must match the rest of the site.
        var size = analysis.Dimensions.HasBothSides
            ? Invariant($"{analysis.Dimensions.LengthMeters:0.#} × {analysis.Dimensions.WidthMeters:0.#} m")
            : Invariant($"{analysis.Dimensions.AreaSquareMeters:0.#} m²");

        var area = Invariant($"{plan.AreaSquareMeters:0.#}");
        var lumens = Invariant($"{plan.TotalLumensRequired:N0}");

        text.AppendLine(L(arabic,
            $"For a **{size}** {room.GetName(false)} ({area} m²), here's what I'd fit:",
            $"لـ**{room.GetName(true)}** بمقاس **{size}** ({area} م²)، إليك ما أنصح به:"));
        text.AppendLine();

        text.AppendLine(L(arabic,
            $"- **{plan.FixtureCount} × {fixture.GetName(false)}** at **{plan.WattsPerFixture}W** each ({plan.TotalWatts}W total)",
            $"- **{plan.FixtureCount} × {fixture.GetName(true)}** بقدرة **{plan.WattsPerFixture} واط** لكل وحدة (الإجمالي {plan.TotalWatts} واط)"));

        text.AppendLine(L(arabic,
            $"- Colour temperature: **{plan.ColorTemperatureLabel}**",
            $"- درجة حرارة اللون: **{plan.ColorTemperatureLabel}**"));

        if (!string.IsNullOrEmpty(plan.SuggestedLayout))
        {
            text.AppendLine(L(arabic,
                $"- Layout: **{plan.SuggestedLayout}** grid, evenly spaced",
                $"- التوزيع: شبكة **{plan.SuggestedLayout}** بتباعد متساوٍ"));
        }

        text.AppendLine();
        text.AppendLine(L(arabic,
            $"_Based on a {plan.LuxTarget} lux target for a {room.GetName(false)}, which needs about {lumens} lumens in total._",
            $"_محسوبة على مستوى إضاءة {plan.LuxTarget} لوكس المناسب لـ{room.GetName(true)}، أي نحو {lumens} لومن إجمالًا._"));

        var response = new ChatProviderResponse
        {
            Intent = ChatIntent.LightingPlan,
            Keywords = analysis.Keywords,
            Text = text.ToString().TrimEnd()
        };

        response.Payload.LightingPlan = plan;

        var products = await _recommendationService.RecommendForPlanAsync(
            fixture, plan.WattsPerFixture, request.StoreId, request.LanguageId, request.MaxProducts);

        if (products.Count > 0)
            response.Payload.Products = await _productSearchService.PrepareCardsAsync(products);

        response.Payload.FollowUps = FollowUps(arabic,
            L(arabic, "How many LED strips for the same room?", "كم متر شريط ليد لنفس الغرفة؟"));

        return response;
    }

    /// <summary>
    /// Answers a count: "how many spotlights do I need?".
    /// </summary>
    protected virtual async Task<ChatProviderResponse> AnswerQuantityAsync(IntentAnalysis analysis, ChatProviderRequest request, bool arabic)
    {
        var fixture = analysis.Need ?? NeedCatalog.FindBySlug("spotlight");
        var estimate = _lightingCalculator.EstimateQuantity(fixture, analysis.Room, analysis.Dimensions, arabic);

        var text = new StringBuilder();
        text.AppendLine(L(arabic,
            $"You'll need roughly **{estimate.Quantity} {estimate.Unit}** of {estimate.ItemLabel}.",
            $"ستحتاج تقريبًا **{estimate.Quantity} {estimate.Unit}** من {estimate.ItemLabel}."));
        text.AppendLine();
        text.AppendLine(estimate.Explanation);

        var response = new ChatProviderResponse
        {
            Intent = ChatIntent.QuantityEstimation,
            Keywords = analysis.Keywords,
            Text = text.ToString().TrimEnd()
        };

        response.Payload.QuantityEstimate = estimate;

        var products = await _recommendationService.RecommendAsync(fixture, request.StoreId, request.LanguageId, request.MaxProducts);
        if (products.Count > 0)
            response.Payload.Products = await _productSearchService.PrepareCardsAsync(products);

        response.Payload.FollowUps = FollowUps(arabic,
            L(arabic, "What wattage should I choose?", "ما القدرة المناسبة؟"));

        return response;
    }

    /// <summary>
    /// Answers from the store's knowledge base.
    /// </summary>
    protected virtual ChatProviderResponse AnswerFaq(ChatFaq faq, IntentAnalysis analysis, bool arabic)
    {
        var response = new ChatProviderResponse
        {
            Intent = ChatIntent.Faq,
            Keywords = analysis.Keywords,
            MatchedFaqId = faq.Id,
            //the answer is authored by the store owner in markdown; the browser escapes HTML before
            //rendering it, so an admin cannot inject script into a shopper's page even by accident
            Text = faq.Answer
        };

        response.Payload.FollowUps = FollowUps(arabic,
            L(arabic, "What's your return policy?", "ما هي سياسة الإرجاع؟"));

        return response;
    }

    /// <summary>
    /// Answers a request for products of a known kind, or a free-text catalogue search.
    /// </summary>
    protected virtual async Task<ChatProviderResponse> AnswerProductsAsync(IntentAnalysis analysis, ChatProviderRequest request, bool arabic)
    {
        IList<Product> products;
        string heading;

        if (analysis.Need is not null)
        {
            products = await _recommendationService.RecommendAsync(
                analysis.Need, request.StoreId, request.LanguageId, request.MaxProducts);

            heading = L(arabic,
                $"Here's what I'd recommend from our {analysis.Need.GetName(false)}:",
                $"إليك ما أرشحه لك من {analysis.Need.GetName(true)}:");
        }
        else
        {
            products = await _productSearchService.SearchAsync(new ProductSearchQuery
            {
                Keywords = analysis.SearchQuery,
                StoreId = request.StoreId,
                LanguageId = request.LanguageId,
                PageSize = request.MaxProducts
            });

            heading = L(arabic,
                $"Here's what I found for **{analysis.SearchQuery}**:",
                $"إليك ما وجدته لـ**{analysis.SearchQuery}**:");
        }

        if (products.Count == 0)
            return NotFound(analysis, arabic);

        var response = new ChatProviderResponse
        {
            Intent = analysis.Need is not null ? ChatIntent.ProductRecommendation : ChatIntent.ProductSearch,
            Keywords = analysis.Keywords,
            Text = heading
        };

        response.Payload.Products = await _productSearchService.PrepareCardsAsync(products);
        response.Payload.FollowUps = FollowUps(arabic,
            L(arabic, "Compare the first two", "قارن بين أول اثنين"));

        return response;
    }

    /// <summary>
    /// Says so, when the catalogue has nothing. Every one of these is logged as an unanswered question, so
    /// the store owner can see the demand they are missing.
    /// </summary>
    protected virtual ChatProviderResponse NotFound(IntentAnalysis analysis, bool arabic)
    {
        var response = new ChatProviderResponse
        {
            //recorded as Unknown, not as a failed search: this is the number the analytics page reports as
            //"unanswered", and it is the most actionable figure the store owner has
            Intent = ChatIntent.Unknown,
            Keywords = analysis.Keywords,
            Text = L(arabic,
                "I couldn't find anything matching that. I can help you with:\n\n" +
                "- Finding products — *\"I need outdoor lighting\"*\n" +
                "- Planning a room — *\"my room is 5 x 4\"*\n" +
                "- Comparing two products — *\"compare A and B\"*\n" +
                "- Shipping, returns, warranty and branch questions",

                "لم أجد ما يطابق طلبك. يمكنني مساعدتك في:\n\n" +
                "- إيجاد المنتجات — *\"أحتاج إنارة خارجية\"*\n" +
                "- تخطيط إنارة غرفة — *\"غرفتي 5 × 4\"*\n" +
                "- المقارنة بين منتجين — *\"قارن بين A و B\"*\n" +
                "- أسئلة الشحن والإرجاع والضمان والفروع")
        };

        response.Payload.FollowUps = FollowUps(arabic,
            L(arabic, "I need a chandelier", "أحتاج ثريا"),
            L(arabic, "I need CCTV", "أحتاج كاميرات مراقبة"));

        return response;
    }

    #endregion

    #region Methods

    /// <summary>
    /// The built-in provider needs nothing configured, so it is always available. A provider with an API
    /// key returns false here until that key is set.
    /// </summary>
    public Task<bool> IsConfiguredAsync()
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Answers one question
    /// </summary>
    public virtual async Task<ChatProviderResponse> GetResponseAsync(ChatProviderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var analysis = _intentRecognizer.Analyze(request.Message);
        var arabic = UseArabic(analysis, request);

        switch (analysis.Intent)
        {
            case ChatIntent.Greeting:
            {
                var welcome = arabic ? _settings.WelcomeMessageAr : _settings.WelcomeMessage;

                var response = ChatProviderResponse.FromText(
                    !string.IsNullOrWhiteSpace(welcome)
                        ? welcome
                        : L(arabic,
                            "Hello! I'm the Noor lighting assistant. Tell me what you're looking for, or give me your room size and I'll work out what you need.",
                            "أهلًا بك! أنا مساعد نور للإنارة. أخبرني بما تبحث عنه، أو أعطني مقاس غرفتك وسأحسب لك ما تحتاجه."),
                    ChatIntent.Greeting);

                response.Payload.FollowUps = FollowUps(arabic,
                    L(arabic, "I need a chandelier", "أحتاج ثريا"));

                return response;
            }

            case ChatIntent.SmallTalk:
                return ChatProviderResponse.FromText(
                    L(arabic,
                        "Happy to help. Anything else you'd like to find?",
                        "سعيد بمساعدتك. هل هناك شيء آخر تبحث عنه؟"),
                    ChatIntent.SmallTalk);

            case ChatIntent.ProductComparison when request.AllowProductComparison:
                return await AnswerComparisonAsync(analysis, request, arabic);

            case ChatIntent.LightingPlan when request.AllowQuantityEstimation:
                return await AnswerLightingPlanAsync(analysis, request, arabic);

            case ChatIntent.QuantityEstimation when request.AllowQuantityEstimation:
                return await AnswerQuantityAsync(analysis, request, arabic);
        }

        //the knowledge base is consulted after the structural intents but before the catalogue, so
        //"is there a warranty on chandeliers?" is answered by the warranty entry rather than by a shelf of
        //chandeliers. A strong keyword hit is required, so "I need a chandelier" does not trip it.
        var faq = await _chatFaqService.MatchAsync(analysis.NormalizedMessage, request.StoreId, arabic);
        if (faq is not null)
            return AnswerFaq(faq, analysis, arabic);

        //a capability the store owner switched off falls through to here, so the shopper still gets
        //products rather than silence
        var wantsProducts = analysis.Intent is ChatIntent.ProductRecommendation or ChatIntent.ProductSearch
                            or ChatIntent.ProductComparison or ChatIntent.LightingPlan or ChatIntent.QuantityEstimation;

        if (wantsProducts && request.AllowProductRecommendation)
            return await AnswerProductsAsync(analysis, request, arabic);

        return NotFound(analysis, arabic);
    }

    #endregion
}
