namespace Nop.Plugin.Misc.NoorAiAssistant;

/// <summary>
/// Every string the plugin shows, in one place.
/// </summary>
/// <remarks>
/// These are English (the resource table's default language). A store owner translates them per language
/// in Admin → Configuration → Languages → String resources, exactly as for any other nopCommerce string —
/// which is why the chat window's chrome (buttons, placeholders, tooltips) goes through here rather than
/// being hard-coded in the Razor view. The assistant's *answers* are a separate matter: those are written
/// bilingually by the provider and the FAQ table, because they follow the language the shopper types in,
/// not the language of the storefront.
/// </remarks>
public static class NoorAiLocaleResources
{
    private const string P = NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX;

    public static Dictionary<string, string> All => new()
    {
        //--- public: chat window chrome
        [$"{P}.Public.Title"] = "Noor Assistant",
        [$"{P}.Public.Subtitle"] = "Lighting expert · replies instantly",
        [$"{P}.Public.Launcher"] = "Ask the Noor assistant",
        [$"{P}.Public.Placeholder"] = "Ask about products, or give me your room size…",
        [$"{P}.Public.Send"] = "Send",
        [$"{P}.Public.NewChat"] = "New chat",
        [$"{P}.Public.History"] = "Chat history",
        [$"{P}.Public.ClearHistory"] = "Clear all conversations",
        [$"{P}.Public.ClearConfirm"] = "Delete all of your conversations? This cannot be undone.",
        [$"{P}.Public.Close"] = "Close",
        [$"{P}.Public.Copy"] = "Copy",
        [$"{P}.Public.Copied"] = "Copied",
        [$"{P}.Public.Thinking"] = "Thinking…",
        [$"{P}.Public.ViewDetails"] = "View details",
        [$"{P}.Public.AddToCart"] = "Add to cart",
        [$"{P}.Public.InStock"] = "In stock",
        [$"{P}.Public.OutOfStock"] = "Out of stock",
        [$"{P}.Public.Off"] = "off",
        [$"{P}.Public.Recommended"] = "Recommended",
        [$"{P}.Public.NoHistory"] = "No conversations yet.",
        [$"{P}.Public.ToggleTheme"] = "Switch between light and dark",
        [$"{P}.Public.Hint"] = "Enter to send · Shift+Enter for a new line",

        //--- public: errors the API returns
        [$"{P}.Public.Disabled"] = "The assistant is currently unavailable. Please try again later.",
        [$"{P}.Public.EmptyMessage"] = "Please type a message first.",
        [$"{P}.Public.RateLimited"] = "You're sending messages a little too quickly. Please wait a moment and try again.",
        [$"{P}.Public.Error"] = "Sorry — something went wrong on my side. Please try again.",

        //--- admin: menu
        [$"{P}.Menu.Root"] = "AI Assistant",
        [$"{P}.Menu.Settings"] = "Settings",
        [$"{P}.Menu.Faq"] = "FAQ",
        [$"{P}.Menu.SuggestedQuestions"] = "Suggested questions",
        [$"{P}.Menu.Analytics"] = "Analytics",

        //--- admin: settings page
        [$"{P}.Configuration"] = "AI Assistant settings",
        [$"{P}.Configuration.General"] = "General",
        [$"{P}.Configuration.Appearance"] = "Appearance",
        [$"{P}.Configuration.Capabilities"] = "Capabilities",
        [$"{P}.Configuration.Limits"] = "Limits & security",
        [$"{P}.Configuration.Lighting"] = "Lighting calculator",

        [$"{P}.Fields.Enabled"] = "Enabled",
        [$"{P}.Fields.Enabled.Hint"] = "Show the assistant on the storefront. Unchecking this hides the launcher and switches the API off.",
        [$"{P}.Fields.ActiveProviderSystemName"] = "AI provider",
        [$"{P}.Fields.ActiveProviderSystemName.Hint"] = "Which engine answers questions. Only the built-in provider ships with the plugin; adding OpenAI, Claude, Gemini or Ollama means adding one class that implements IChatProvider, after which it appears in this list.",
        [$"{P}.Fields.ThemeColor"] = "Theme colour",
        [$"{P}.Fields.ThemeColor.Hint"] = "The accent colour of the launcher button and the shopper's message bubbles.",
        [$"{P}.Fields.WelcomeMessage"] = "Welcome message (English)",
        [$"{P}.Fields.WelcomeMessage.Hint"] = "Shown at the top of an empty conversation.",
        [$"{P}.Fields.WelcomeMessageAr"] = "Welcome message (Arabic)",
        [$"{P}.Fields.WelcomeMessageAr.Hint"] = "Shown instead of the English one when the shopper is browsing in a right-to-left language.",
        [$"{P}.Fields.MaxHistoryMessages"] = "Maximum history",
        [$"{P}.Fields.MaxHistoryMessages.Hint"] = "How many past messages are replayed into the chat window and passed to the provider as context.",
        [$"{P}.Fields.MaxProductsPerAnswer"] = "Products per answer",
        [$"{P}.Fields.MaxProductsPerAnswer.Hint"] = "How many product cards a single answer may show. Three keeps the window readable on a phone.",

        [$"{P}.Fields.AllowProductRecommendation"] = "Allow product recommendation",
        [$"{P}.Fields.AllowProductRecommendation.Hint"] = "Let the assistant search the catalogue and suggest products.",
        [$"{P}.Fields.AllowProductComparison"] = "Allow product comparison",
        [$"{P}.Fields.AllowProductComparison.Hint"] = "Let shoppers ask \"compare A and B\" and get a side-by-side table with a recommendation.",
        [$"{P}.Fields.AllowQuantityEstimation"] = "Allow quantity estimation",
        [$"{P}.Fields.AllowQuantityEstimation.Hint"] = "Let the assistant answer \"how many spotlights do I need?\" and plan the lighting for a room from its dimensions.",
        [$"{P}.Fields.TrackAnalytics"] = "Track analytics",
        [$"{P}.Fields.TrackAnalytics.Hint"] = "Record what shoppers ask, which keywords they use and which products get recommended, for the Analytics page.",

        [$"{P}.Fields.RateLimitMessages"] = "Rate limit (messages)",
        [$"{P}.Fields.RateLimitMessages.Hint"] = "How many messages one customer may send per window. Set to 0 to switch rate limiting off.",
        [$"{P}.Fields.RateLimitWindowSeconds"] = "Rate limit window (seconds)",
        [$"{P}.Fields.RateLimitWindowSeconds.Hint"] = "The window the message limit applies over.",
        [$"{P}.Fields.MaxMessageLength"] = "Maximum message length",
        [$"{P}.Fields.MaxMessageLength.Hint"] = "Longer messages are truncated rather than rejected.",

        [$"{P}.Fields.DefaultLuxTarget"] = "Default lux target",
        [$"{P}.Fields.DefaultLuxTarget.Hint"] = "The illuminance the lighting planner aims for when the shopper doesn't say what the room is used for. 150 lux suits a living room; a kitchen wants 300.",
        [$"{P}.Fields.DefaultLumensPerWatt"] = "Fixture efficacy (lumens per watt)",
        [$"{P}.Fields.DefaultLumensPerWatt.Hint"] = "The efficiency of the fixtures you sell, used to turn a lumen requirement into a wattage. Modern LED spotlights are 90–110 lm/W.",

        //--- admin: FAQ
        [$"{P}.Faq"] = "Assistant FAQ",
        [$"{P}.Faq.AddNew"] = "Add a new entry",
        [$"{P}.Faq.EditDetails"] = "Edit entry",
        [$"{P}.Faq.BackToList"] = "back to FAQ list",
        [$"{P}.Faq.Added"] = "The FAQ entry has been added successfully.",
        [$"{P}.Faq.Updated"] = "The FAQ entry has been updated successfully.",
        [$"{P}.Faq.Deleted"] = "The FAQ entry has been deleted successfully.",

        [$"{P}.Faq.Fields.Question"] = "Question",
        [$"{P}.Faq.Fields.Question.Hint"] = "The question as a shopper would ask it. Also used for matching when no keywords are given.",
        [$"{P}.Faq.Fields.Answer"] = "Answer",
        [$"{P}.Faq.Fields.Answer.Hint"] = "Markdown is supported: **bold**, *italic*, - bullet lists, and [links](https://example.com). HTML is escaped before it reaches the shopper.",
        [$"{P}.Faq.Fields.Keywords"] = "Keywords",
        [$"{P}.Faq.Fields.Keywords.Hint"] = "Comma-separated words that should route a question here, in every language you support — e.g. \"shipping,delivery,شحن,توصيل\". This is the strongest matching signal, so it is worth filling in.",
        [$"{P}.Faq.Fields.Topic"] = "Topic",
        [$"{P}.Faq.Fields.Topic.Hint"] = "Groups the entry in this list.",
        [$"{P}.Faq.Fields.Language"] = "Language",
        [$"{P}.Faq.Fields.Language.Hint"] = "The language this entry answers in. Choose \"All\" to offer it in every language.",
        [$"{P}.Faq.Fields.Store"] = "Store",
        [$"{P}.Faq.Fields.Store.Hint"] = "The store this entry belongs to. Choose \"All\" for every store.",
        [$"{P}.Faq.Fields.DisplayOrder"] = "Display order",
        [$"{P}.Faq.Fields.Published"] = "Published",
        [$"{P}.Faq.Fields.Published.Hint"] = "Unpublished entries are never matched.",
        [$"{P}.Faq.Fields.SearchKeywords"] = "Search",
        [$"{P}.Faq.Fields.SearchTopic"] = "Topic",

        //--- admin: suggested questions
        [$"{P}.SuggestedQuestions"] = "Suggested questions",
        [$"{P}.SuggestedQuestions.AddNew"] = "Add a new question",
        [$"{P}.SuggestedQuestions.EditDetails"] = "Edit question",
        [$"{P}.SuggestedQuestions.BackToList"] = "back to suggested questions",
        [$"{P}.SuggestedQuestions.Added"] = "The question has been added successfully.",
        [$"{P}.SuggestedQuestions.Updated"] = "The question has been updated successfully.",
        [$"{P}.SuggestedQuestions.Deleted"] = "The question has been deleted successfully.",
        [$"{P}.SuggestedQuestions.Hint"] = "These are the chips a shopper sees in an empty chat window. Pick ones that show off what the assistant can do — a room size, a comparison — rather than only what you sell.",

        [$"{P}.SuggestedQuestions.Fields.Text"] = "Question",
        [$"{P}.SuggestedQuestions.Fields.Text.Hint"] = "Shown as a clickable chip. Keep it short enough to read at a glance.",
        [$"{P}.SuggestedQuestions.Fields.Language"] = "Language",
        [$"{P}.SuggestedQuestions.Fields.Store"] = "Store",
        [$"{P}.SuggestedQuestions.Fields.DisplayOrder"] = "Display order",
        [$"{P}.SuggestedQuestions.Fields.Published"] = "Published",
        [$"{P}.SuggestedQuestions.Fields.SearchKeywords"] = "Search",

        //--- admin: analytics
        [$"{P}.Analytics"] = "Assistant analytics",
        [$"{P}.Analytics.From"] = "From",
        [$"{P}.Analytics.To"] = "To",
        [$"{P}.Analytics.Store"] = "Store",
        [$"{P}.Analytics.Apply"] = "Apply",
        [$"{P}.Analytics.Conversations"] = "Conversations",
        [$"{P}.Analytics.Messages"] = "Messages",
        [$"{P}.Analytics.AverageLength"] = "Avg. conversation length",
        [$"{P}.Analytics.AverageLength.Hint"] = "Messages per conversation. If this sits at 2, shoppers ask once and leave.",
        [$"{P}.Analytics.AverageResponseTime"] = "Avg. response time",
        [$"{P}.Analytics.ProductsRecommended"] = "Products recommended",
        [$"{P}.Analytics.FaqMatches"] = "FAQ answers given",
        [$"{P}.Analytics.Unanswered"] = "Unanswered questions",
        [$"{P}.Analytics.Unanswered.Hint"] = "Questions the assistant could not answer. Each one is a missing FAQ entry or a product you don't stock — this is the most useful number on this page.",
        [$"{P}.Analytics.TopQuestions"] = "Most asked questions",
        [$"{P}.Analytics.TopKeywords"] = "Most searched keywords",
        [$"{P}.Analytics.TopRecommended"] = "Most recommended products",
        [$"{P}.Analytics.TopViewed"] = "Most viewed products",
        [$"{P}.Analytics.Term"] = "Term",
        [$"{P}.Analytics.Product"] = "Product",
        [$"{P}.Analytics.Count"] = "Count",
        [$"{P}.Analytics.NoData"] = "No data for this period yet.",

        //--- admin: shared
        [$"{P}.Common.All"] = "All",
        [$"{P}.Saved"] = "The settings have been saved."
    };
}
