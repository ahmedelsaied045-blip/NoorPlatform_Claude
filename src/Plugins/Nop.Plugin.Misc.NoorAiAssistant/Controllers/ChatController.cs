using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.NoorAiAssistant.Models.Api;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Conversations;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.NoorAiAssistant.Controllers;

/// <summary>
/// The storefront REST API behind the chat window.
/// </summary>
/// <remarks>
/// Thin by design: every action validates its input, calls <see cref="IChatService"/>, and maps the result
/// to a status code. No business logic lives here, which is why the same service can later be driven by a
/// mobile app or a webhook without any of this being reimplemented.
///
/// Security posture:
/// - inherits BasePublicController, so the store's access rules (closed store, SSL, affiliate) still apply;
/// - [AutoValidateAntiforgeryToken] means the mutating verbs (POST, DELETE) require nopCommerce's
///   antiforgery token, which the storefront already emits on every page;
/// - [CheckLanguageSeoCode(ignore: true)] stops the language filter from 302-redirecting XHRs to a
///   /{lang}/ URL, which would silently turn a POST into a GET;
/// - rate limiting, length capping and HTML stripping happen inside ChatService, so they cannot be
///   bypassed by calling the service from somewhere else.
/// </remarks>
[AutoValidateAntiforgeryToken]
public class ChatController : BasePublicController
{
    #region Fields

    private readonly IChatService _chatService;

    #endregion

    #region Ctor

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// POST /api/chat/send — ask the assistant something.
    /// </summary>
    [HttpPost]
    [CheckLanguageSeoCode(ignore: true)]
    public virtual async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                error = string.Join(' ', ModelState.Values
                    .SelectMany(state => state.Errors)
                    .Select(error => error.ErrorMessage))
            });
        }

        var result = await _chatService.SendAsync(request);

        if (!result.Success)
            return StatusCode(result.StatusCode, new { error = result.Error });

        return Ok(result.Response);
    }

    /// <summary>
    /// GET /api/chat/history — replay a conversation and list the shopper's others.
    /// </summary>
    [HttpGet]
    [CheckLanguageSeoCode(ignore: true)]
    public virtual async Task<IActionResult> History(Guid? sessionId)
    {
        return Ok(await _chatService.GetHistoryAsync(sessionId));
    }

    /// <summary>
    /// DELETE /api/chat/history — clear the shopper's conversations.
    /// </summary>
    [HttpDelete]
    [CheckLanguageSeoCode(ignore: true)]
    public virtual async Task<IActionResult> ClearHistory()
    {
        await _chatService.ClearHistoryAsync();

        return Ok(new { cleared = true });
    }

    /// <summary>
    /// GET /api/chat/suggestions — the greeting and starter chips for an empty conversation.
    /// </summary>
    [HttpGet]
    [CheckLanguageSeoCode(ignore: true)]
    public virtual async Task<IActionResult> Suggestions()
    {
        return Ok(await _chatService.GetSuggestionsAsync());
    }

    /// <summary>
    /// GET /api/chat/search-products — search the catalogue from inside the chat window.
    /// </summary>
    [HttpGet]
    [CheckLanguageSeoCode(ignore: true)]
    public virtual async Task<IActionResult> SearchProducts(string q, int pageSize = 6)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new ProductSearchResponse());

        var products = await _chatService.SearchProductsAsync(q, pageSize);

        return Ok(new ProductSearchResponse { Query = q, Products = products });
    }

    /// <summary>
    /// POST /api/chat/track-view — record a click-through from a product card, for the "most viewed
    /// products" report. Fire-and-forget from the browser; a failure here must never block navigation.
    /// </summary>
    [HttpPost]
    [CheckLanguageSeoCode(ignore: true)]
    public virtual async Task<IActionResult> TrackView(Guid sessionId, int productId)
    {
        await _chatService.TrackProductViewAsync(sessionId, productId);

        return Ok();
    }

    #endregion
}
