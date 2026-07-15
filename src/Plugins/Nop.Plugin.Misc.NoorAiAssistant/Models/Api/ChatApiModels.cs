using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models.Api;

/*
 * On serialisation: nopCommerce configures MVC with AddNewtonsoftJson + DefaultContractResolver, so JSON
 * goes over the wire in PascalCase and System.Text.Json's [JsonPropertyName] is ignored entirely. Since the
 * browser expects camelCase, every DTO that crosses the wire is tagged with the camel-case naming strategy
 * below. The same attribute also governs how the payload is written into ChatMessage.PayloadJson, so a
 * message replayed from history deserialises into exactly the shape a fresh one arrives in.
 */

/// <summary>
/// The body of POST /api/chat/send.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class SendMessageRequest
{
    /// <summary>
    /// Gets or sets the conversation to append to. Omit to start a new one.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the shopper's message. The length cap is asserted again server-side against the
    /// configured maximum; this attribute only rejects the absurd before any work is done.
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Message { get; set; }
}

/// <summary>
/// One message as the browser sees it.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ChatMessageDto
{
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets "user" or "assistant".
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// Gets or sets the message body. Markdown for assistant messages, plain text for the shopper's.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the rich content (product cards, comparison table, lighting plan). Null on plain
    /// answers and on every user message.
    /// </summary>
    public ChatAnswerPayload Payload { get; set; }

    [JsonProperty("createdOn")]
    public DateTime CreatedOnUtc { get; set; }
}

/// <summary>
/// The reply to POST /api/chat/send.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class SendMessageResponse
{
    /// <summary>
    /// Gets or sets the conversation the message landed in. The browser stores this and sends it back on
    /// the next turn.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets the assistant's reply.
    /// </summary>
    public ChatMessageDto Reply { get; set; }
}

/// <summary>
/// The reply to GET /api/chat/history.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ChatHistoryResponse
{
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the messages of the requested conversation, oldest first.
    /// </summary>
    public IList<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();

    /// <summary>
    /// Gets or sets the shopper's other conversations, for the history panel.
    /// </summary>
    public IList<ChatSessionSummaryDto> Sessions { get; set; } = new List<ChatSessionSummaryDto>();
}

/// <summary>
/// One row in the conversation history panel.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ChatSessionSummaryDto
{
    public Guid SessionId { get; set; }

    public string Title { get; set; }

    public int MessageCount { get; set; }

    [JsonProperty("lastActivityOn")]
    public DateTime LastActivityOnUtc { get; set; }
}

/// <summary>
/// The reply to GET /api/chat/suggestions.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class SuggestionsResponse
{
    /// <summary>
    /// Gets or sets the greeting shown above the chips.
    /// </summary>
    public string Welcome { get; set; }

    public IList<string> Suggestions { get; set; } = new List<string>();
}

/// <summary>
/// The reply to GET /api/chat/search-products.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ProductSearchResponse
{
    public string Query { get; set; }

    public IList<ProductCardDto> Products { get; set; } = new List<ProductCardDto>();
}

/// <summary>
/// What <see cref="Services.Conversations.IChatService.SendAsync"/> gives back: either an answer, or a
/// reason it refused. Modelled as a result rather than an exception because a rate-limited or over-long
/// message is an expected outcome on a public endpoint, not an exceptional one.
/// </summary>
public class ChatSendResult
{
    public bool Success { get; init; }

    /// <summary>
    /// Gets the HTTP status the controller should return. 200 on success, 400 for a bad message, 429 when
    /// rate limited, 503 when the assistant is switched off.
    /// </summary>
    public int StatusCode { get; init; } = StatusCodes.Status200OK;

    /// <summary>
    /// Gets the message shown to the shopper when <see cref="Success"/> is false. Already localised.
    /// </summary>
    public string Error { get; init; }

    public SendMessageResponse Response { get; init; }

    public static ChatSendResult Ok(SendMessageResponse response)
    {
        return new ChatSendResult { Success = true, Response = response };
    }

    public static ChatSendResult Fail(int statusCode, string error)
    {
        return new ChatSendResult { Success = false, StatusCode = statusCode, Error = error };
    }
}
