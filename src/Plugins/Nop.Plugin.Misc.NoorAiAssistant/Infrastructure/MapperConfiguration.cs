using AutoMapper;
using Nop.Core.Infrastructure.Mapper;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Models.Admin;

namespace Nop.Plugin.Misc.NoorAiAssistant.Infrastructure;

/// <summary>
/// AutoMapper configuration for the plugin's admin models.
/// </summary>
public class MapperConfiguration : Profile, IOrderedMapperProfile
{
    #region Ctor

    public MapperConfiguration()
    {
        //--- settings
        CreateMap<NoorAiAssistantSettings, ConfigurationModel>()
            .ForMember(model => model.ActiveStoreScopeConfiguration, options => options.Ignore())
            .ForMember(model => model.AvailableProviders, options => options.Ignore())
            .ForMember(model => model.Enabled_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.ActiveProviderSystemName_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.ThemeColor_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.WelcomeMessage_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.WelcomeMessageAr_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.MaxHistoryMessages_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.MaxProductsPerAnswer_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.AllowProductRecommendation_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.AllowProductComparison_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.AllowQuantityEstimation_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.TrackAnalytics_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.RateLimitMessages_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.RateLimitWindowSeconds_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.MaxMessageLength_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.DefaultLuxTarget_OverrideForStore, options => options.Ignore())
            .ForMember(model => model.DefaultLumensPerWatt_OverrideForStore, options => options.Ignore());

        CreateMap<ConfigurationModel, NoorAiAssistantSettings>();

        //--- FAQ
        CreateMap<ChatFaq, FaqModel>()
            .ForMember(model => model.TopicName, options => options.Ignore())
            .ForMember(model => model.LanguageName, options => options.Ignore())
            .ForMember(model => model.AvailableTopics, options => options.Ignore())
            .ForMember(model => model.AvailableLanguages, options => options.Ignore())
            .ForMember(model => model.AvailableStores, options => options.Ignore());

        CreateMap<FaqModel, ChatFaq>()
            //timestamps are owned by the service, not by whatever the form posted
            .ForMember(entity => entity.CreatedOnUtc, options => options.Ignore())
            .ForMember(entity => entity.UpdatedOnUtc, options => options.Ignore())
            .ForMember(entity => entity.Topic, options => options.Ignore());

        //--- suggested questions
        CreateMap<SuggestedQuestion, SuggestedQuestionModel>()
            .ForMember(model => model.LanguageName, options => options.Ignore())
            .ForMember(model => model.AvailableLanguages, options => options.Ignore())
            .ForMember(model => model.AvailableStores, options => options.Ignore());

        CreateMap<SuggestedQuestionModel, SuggestedQuestion>();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Order of this mapper implementation
    /// </summary>
    public int Order => 1;

    #endregion
}
