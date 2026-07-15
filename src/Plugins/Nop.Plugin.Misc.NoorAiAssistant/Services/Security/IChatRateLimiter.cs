namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Security;

/// <summary>
/// Caps how fast one customer can send messages.
/// </summary>
/// <remarks>
/// The chat endpoint runs a catalogue search per message and, once a real model is plugged in, will cost
/// money per message. Both make it worth defending. The limit is per customer rather than per IP because
/// nopCommerce gives every visitor — guest included — a customer record, and an IP is shared by everyone
/// behind one office NAT.
/// </remarks>
public interface IChatRateLimiter
{
    /// <summary>
    /// Counts a request against the customer's allowance.
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result is true when the request is allowed, false when the customer has exceeded the limit.
    /// </returns>
    Task<bool> TryAcquireAsync(int customerId);
}
