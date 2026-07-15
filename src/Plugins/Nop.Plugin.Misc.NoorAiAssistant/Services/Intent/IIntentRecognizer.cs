namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Intent;

/// <summary>
/// Works out what a shopper is asking for, from the text alone.
/// </summary>
public interface IIntentRecognizer
{
    /// <summary>
    /// Reads a message.
    /// </summary>
    /// <param name="message">The raw message as typed</param>
    /// <returns>What the message is about</returns>
    IntentAnalysis Analyze(string message);
}
