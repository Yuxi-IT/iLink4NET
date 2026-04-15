using System.Text.Json;
using System.Text.Json.Serialization;
using ILink4NET.Exceptions;
using ILink4NET.Models;
using ILink4NET.Options;
using ILink4NET.Stores;
using ILink4NET.Transport;

namespace ILink4NET.Messaging;

/// <summary>
/// iLink 消息收发实现。
/// </summary>
public sealed class MessageService : IMessageService
{
    private readonly ILinkHttpClient _httpClient;
    private readonly IContextTokenStore _contextTokenStore;
    private readonly ILinkClientOptions _options;

    public MessageService(ILinkHttpClient httpClient, IContextTokenStore contextTokenStore, ILinkClientOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _contextTokenStore = contextTokenStore ?? throw new ArgumentNullException(nameof(contextTokenStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<UpdateBatch> GetUpdatesAsync(string botToken, string cursor, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);

        var response = await _httpClient.PostAsync<GetUpdatesResponse>(
            "/getupdates",
            new
            {
                base_info = new BaseInfo(_options.ChannelVersion),
                get_updates_buf = cursor,
            },
            botToken,
            cancellationToken).ConfigureAwait(false);

        EnsureResponseState(response.Ret, response.ErrCode, response.ErrorMessage);

        var messages = new List<IncomingMessage>();
        foreach (var rawMessage in response.Messages)
        {
            var userId = rawMessage.ToUserId ?? rawMessage.FromUserId;
            var contextToken = rawMessage.ContextToken;
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(contextToken))
            {
                continue;
            }

            await _contextTokenStore.SetAsync(userId, contextToken, cancellationToken).ConfigureAwait(false);
            messages.Add(new IncomingMessage(userId, contextToken, rawMessage.Text, rawMessage.Raw));
        }

        return new UpdateBatch(response.NextCursor ?? cursor, messages);
    }

    public async Task SendTextMessageAsync(string botToken, OutgoingTextMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentNullException.ThrowIfNull(message);

        var response = await _httpClient.PostAsync<SimpleResponse>(
            "/sendmessage",
            new
            {
                base_info = new BaseInfo(_options.ChannelVersion),
                msg = new
                {
                    to_user_id = message.ToUserId,
                    context_token = message.ContextToken,
                    text = message.Text,
                },
            },
            botToken,
            cancellationToken).ConfigureAwait(false);

        EnsureResponseState(response.Ret, response.ErrCode, response.ErrorMessage);
    }

    public async Task SendReplyAsync(string botToken, IncomingMessage incomingMessage, string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(incomingMessage);

        var token = incomingMessage.ContextToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            token = await _contextTokenStore.GetAsync(incomingMessage.UserId, cancellationToken).ConfigureAwait(false)
                ?? throw new ILinkApiException($"用户 {incomingMessage.UserId} 缺少可用 context_token。");
        }

        await SendTextMessageAsync(
            botToken,
            new OutgoingTextMessage(incomingMessage.UserId, token, text),
            cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureResponseState(int? ret, int? errCode, string? errorMessage)
    {
        if (ret == 0)
        {
            return;
        }

        if (ret == ILinkSessionExpiredException.SessionExpiredCode || errCode == ILinkSessionExpiredException.SessionExpiredCode)
        {
            throw new ILinkSessionExpiredException();
        }

        throw new ILinkApiException(errorMessage ?? "iLink 调用失败。", errCode ?? ret);
    }

    private sealed class GetUpdatesResponse
    {
        [JsonPropertyName("ret")]
        public int? Ret { get; init; }

        [JsonPropertyName("errcode")]
        public int? ErrCode { get; init; }

        [JsonPropertyName("errmsg")]
        public string? ErrorMessage { get; init; }

        [JsonPropertyName("get_updates_buf")]
        public string? NextCursor { get; init; }

        [JsonPropertyName("msgs")]
        public IReadOnlyList<IncomingRawMessage> Messages { get; init; } = [];
    }

    private sealed class IncomingRawMessage
    {
        [JsonPropertyName("from_user_id")]
        public string? FromUserId { get; init; }

        [JsonPropertyName("to_user_id")]
        public string? ToUserId { get; init; }

        [JsonPropertyName("context_token")]
        public string? ContextToken { get; init; }

        [JsonPropertyName("text")]
        public string? Text { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];

        public JsonElement Raw => JsonSerializer.SerializeToElement(ExtensionData);
    }

    private sealed class SimpleResponse
    {
        [JsonPropertyName("ret")]
        public int? Ret { get; init; }

        [JsonPropertyName("errcode")]
        public int? ErrCode { get; init; }

        [JsonPropertyName("errmsg")]
        public string? ErrorMessage { get; init; }
    }
}
