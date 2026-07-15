using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.NoorAiAssistant.Factories;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Analytics;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Catalog;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Conversations;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Faq;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Intent;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Lighting;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Providers;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Recommendation;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Security;

namespace Nop.Plugin.Misc.NoorAiAssistant.Infrastructure;

/// <summary>
/// Registers the plugin's services.
/// </summary>
public class PluginNopStartup : INopStartup
{
    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        //--- chat providers.
        //Registered as a collection, not as a single service: ChatService resolves IEnumerable<IChatProvider>
        //and picks by system name from the settings. Adding OpenAI / Azure OpenAI / Claude / Gemini /
        //Ollama / DeepSeek later is one more AddScoped line here plus the provider class — nothing else in
        //the plugin changes.
        services.AddScoped<IChatProvider, DummyChatProvider>();

        //--- the tools a provider answers with. These stay the same whichever provider is active; a real
        //model would call them as functions rather than the intent engine calling them directly.
        services.AddScoped<IIntentRecognizer, IntentRecognizer>();
        services.AddScoped<ILightingCalculator, LightingCalculator>();
        services.AddScoped<IProductSearchService, ProductSearchService>();
        services.AddScoped<IProductComparisonService, ProductComparisonService>();
        services.AddScoped<IRecommendationService, RecommendationService>();

        //--- persistence and orchestration
        services.AddScoped<IChatFaqService, ChatFaqService>();
        services.AddScoped<ISuggestedQuestionService, SuggestedQuestionService>();
        services.AddScoped<IChatSessionService, ChatSessionService>();
        services.AddScoped<IChatAnalyticsService, ChatAnalyticsService>();
        services.AddScoped<IChatRateLimiter, ChatRateLimiter>();
        services.AddScoped<IChatService, ChatService>();

        //--- admin
        services.AddScoped<INoorAiModelFactory, NoorAiModelFactory>();
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {
    }

    /// <summary>
    /// Gets order of this startup configuration implementation
    /// </summary>
    public int Order => 3000;
}
