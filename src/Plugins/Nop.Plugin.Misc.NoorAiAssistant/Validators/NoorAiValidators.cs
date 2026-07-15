using FluentValidation;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Models.Admin;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Misc.NoorAiAssistant.Validators;

/// <summary>
/// Validates a FAQ entry. FluentValidation validators are discovered by assembly scan, so none of these
/// need registering.
/// </summary>
public class FaqValidator : BaseNopValidator<FaqModel>
{
    public FaqValidator(ILocalizationService localizationService)
    {
        RuleFor(model => model.Question)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Common.Required"));

        RuleFor(model => model.Answer)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Common.Required"));

        //the DB columns are Question nvarchar(450) and Keywords nvarchar(1000); this reads the lengths off
        //the schema rather than restating them, so the two cannot drift apart
        SetDatabaseValidationRules<ChatFaq>();
    }
}

/// <summary>
/// Validates a suggested question.
/// </summary>
public class SuggestedQuestionValidator : BaseNopValidator<SuggestedQuestionModel>
{
    public SuggestedQuestionValidator(ILocalizationService localizationService)
    {
        RuleFor(model => model.Text)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Common.Required"));

        SetDatabaseValidationRules<SuggestedQuestion>();
    }
}

/// <summary>
/// Validates the settings page.
/// </summary>
/// <remarks>
/// The bounds here are not arbitrary: a 0-lux target or a 0 lm/W efficacy would make the lighting
/// calculator divide by zero or return a nonsense fixture count, and a shopper would be told to buy
/// 200,000 spotlights. Rejecting the input is the only place that can be prevented cleanly.
/// </remarks>
public class ConfigurationValidator : BaseNopValidator<ConfigurationModel>
{
    public ConfigurationValidator(ILocalizationService localizationService)
    {
        RuleFor(model => model.MaxHistoryMessages)
            .InclusiveBetween(2, 100)
            .WithMessage("History must be between 2 and 100 messages.");

        RuleFor(model => model.MaxProductsPerAnswer)
            .InclusiveBetween(1, 12)
            .WithMessage("An answer may carry between 1 and 12 product cards.");

        RuleFor(model => model.MaxMessageLength)
            .InclusiveBetween(50, 2000)
            .WithMessage("Message length must be between 50 and 2000 characters.");

        RuleFor(model => model.RateLimitMessages)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Use 0 to switch rate limiting off, or a positive number of messages.");

        RuleFor(model => model.RateLimitWindowSeconds)
            .InclusiveBetween(1, 3600)
            .When(model => model.RateLimitMessages > 0)
            .WithMessage("The rate limit window must be between 1 second and 1 hour.");

        RuleFor(model => model.DefaultLuxTarget)
            .InclusiveBetween(50, 1000)
            .WithMessage("A lux target below 50 or above 1000 is outside anything a room would use.");

        RuleFor(model => model.DefaultLumensPerWatt)
            .InclusiveBetween(10, 250)
            .WithMessage("Fixture efficacy must be between 10 and 250 lm/W. Modern LEDs are 90–110.");

        RuleFor(model => model.ThemeColor)
            .Matches("^#(?:[0-9a-fA-F]{3}){1,2}$")
            .When(model => !string.IsNullOrWhiteSpace(model.ThemeColor))
            //the colour is written straight into a CSS custom property, so it must be a colour and
            //nothing else
            .WithMessage("The theme colour must be a hex value, e.g. #C9A227.");
    }
}
