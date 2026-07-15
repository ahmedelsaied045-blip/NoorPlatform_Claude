namespace Nop.Plugin.Misc.NoorAiAssistant.Domain;

/// <summary>
/// Represents an analytics fact recorded while answering. One question can emit several
/// (e.g. a question asked + three products recommended).
/// </summary>
public enum ChatEventType
{
    /// <summary>
    /// A shopper question was answered. Carries the normalised question text and the response time.
    /// </summary>
    QuestionAsked = 10,

    /// <summary>
    /// A search keyword was extracted from the question.
    /// </summary>
    KeywordSearched = 20,

    /// <summary>
    /// A product was surfaced as a card in an answer.
    /// </summary>
    ProductRecommended = 30,

    /// <summary>
    /// The shopper clicked through to a product from a card.
    /// </summary>
    ProductViewed = 40,

    /// <summary>
    /// A FAQ entry was matched and returned.
    /// </summary>
    FaqMatched = 50
}
