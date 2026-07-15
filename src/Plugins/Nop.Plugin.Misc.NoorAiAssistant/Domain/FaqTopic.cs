namespace Nop.Plugin.Misc.NoorAiAssistant.Domain;

/// <summary>
/// Represents the topic a FAQ entry belongs to. Used to group entries in the admin grid and to let the
/// intent engine route a question to the right subset of entries.
/// </summary>
public enum FaqTopic
{
    Other = 0,
    Shipping = 10,
    Returns = 20,
    Warranty = 30,
    PaymentMethods = 40,
    DeliveryTime = 50,
    Branches = 60,
    WorkingHours = 70,
    CompanyInformation = 80,
    ContactInformation = 90
}
