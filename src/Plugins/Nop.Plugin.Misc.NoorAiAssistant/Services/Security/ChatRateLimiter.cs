using Nop.Core.Caching;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Security;

/// <summary>
/// A counter held in nopCommerce's cache: N messages per window, per customer.
/// </summary>
/// <remarks>
/// The window is refreshed by each *allowed* message, so in practice it behaves as "N messages per window
/// of continuous activity", and a customer who hits the ceiling is free again one window after their last
/// accepted message — being blocked does not extend the block. That is a couple of cache operations rather
/// than a stored list of timestamps, and it runs on whatever cache the store is configured with, so the
/// limit holds across a Redis-backed web farm instead of being per-instance.
/// </remarks>
public partial class ChatRateLimiter : IChatRateLimiter
{
    #region Fields

    private readonly IStaticCacheManager _staticCacheManager;
    private readonly NoorAiAssistantSettings _settings;

    #endregion

    #region Ctor

    public ChatRateLimiter(IStaticCacheManager staticCacheManager,
        NoorAiAssistantSettings settings)
    {
        _staticCacheManager = staticCacheManager;
        _settings = settings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Counts a request against the customer's allowance
    /// </summary>
    public virtual async Task<bool> TryAcquireAsync(int customerId)
    {
        var limit = _settings.RateLimitMessages;
        var windowSeconds = _settings.RateLimitWindowSeconds;

        //a limit of zero or less is how a store owner switches rate limiting off
        if (limit <= 0 || windowSeconds <= 0)
            return true;

        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
            NoorAiAssistantDefaults.RateLimitCacheKey, customerId);

        cacheKey.CacheTime = Math.Max(1, (int)Math.Ceiling(windowSeconds / 60d));

        var count = await _staticCacheManager.GetAsync(cacheKey, () => 0);
        if (count >= limit)
            return false;

        await _staticCacheManager.SetAsync(cacheKey, count + 1);

        return true;
    }

    #endregion
}
