using System.Text.Json;
using System.Text.Json.Serialization;
using ILink4NET.Exceptions;
using ILink4NET.Models;
using ILink4NET.Options;
using ILink4NET.Stores;
using ILink4NET.Transport;

namespace ILink4NET.Messaging;

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
        GetUpdatesResponse response;
        try
        {
            response = await _httpClient.PostAsync<GetUpdatesResponse>(
                "/ilink/bot/getupdates",
                new
                {
                    base_info = new BaseInfo(_options.ChannelVersion),
                    get_updates_buf = cursor,
                },
                botToken,
                cancellationToken).ConfigureAwait(false);
        }
        catch (ILinkApiException ex)
        {
            throw new ILinkApiException($"获取更新失败：{ex.Message}", ex.ErrCode);
        }

        EnsureResponseState(response.Ret, response.ErrCode, response.ErrorMessage);

        var messages = new List<IncomingMessage>();
        foreach (var rawMessage in response.Messages)
        {
            var userId = rawMessage.FromUserId ?? rawMessage.ToUserId;
            var contextToken = rawMessage.ContextToken;
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(contextToken))
            {
                continue;
            }

            await _contextTokenStore.SetAsync(userId, contextToken, cancellationToken).ConfigureAwait(false);
            messages.Add(new IncomingMessage(userId, contextToken, ExtractText(rawMessage), rawMessage.Raw));
        }

        return new UpdateBatch(response.NextCursor ?? cursor, messages);
    }

    public async Task SendTextMessageAsync(string botToken, OutgoingTextMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentNullException.ThrowIfNull(message);

        var response = await _httpClient.PostAsync<SimpleResponse>(
            "/ilink/bot/sendmessage",
            new
            {
                base_info = new BaseInfo(_options.ChannelVersion),
                msg = new
                {
                    from_user_id = string.Empty,
                    to_user_id = message.ToUserId,
                    client_id = $"cs-{Guid.NewGuid():N}",
                    message_type = 2,
                    message_state = 2,
                    context_token = message.ContextToken,
                    item_list = new object[]
                    {
                        new
                        {
                            type = 1,
                            text_item = new { text = message.Text },
                        },
                    },
                },
            },
            botToken,
            cancellationToken).ConfigureAwait(false);

        EnsureResponseState(response.Ret, response.ErrCode, response.ErrorMessage);
    }

    public async Task SendMediaMessageAsync(string botToken, OutgoingMediaMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentNullException.ThrowIfNull(message);

        var mediaObject = new
        {
            encrypt_query_param = message.MediaReference.EncryptQueryParam,
            aes_key = message.MediaReference.AesKey,
            encrypt_type = 1,
        };

        object item = message.MediaType switch
        {
            ILink4NET.Media.MediaType.Image => new
            {
                type = 2,
                image_item = new { media = mediaObject },
            },
            ILink4NET.Media.MediaType.Voice => new
            {
                type = 3,
                voice_item = new { media = mediaObject },
            },
            ILink4NET.Media.MediaType.Video => new
            {
                type = 5,
                video_item = new { media = mediaObject },
            },
            ILink4NET.Media.MediaType.File => new
            {
                type = 4,
                file_item = new
                {
                    media = mediaObject,
                    file_name = message.MediaReference.FileName ?? string.Empty,
                    len = message.MediaReference.FileSize?.ToString() ?? "0",
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(message), message.MediaType, "不支持的媒体类型。"),
        };

        var response = await _httpClient.PostAsync<SimpleResponse>(
            "/ilink/bot/sendmessage",
            new
            {
                base_info = new BaseInfo(_options.ChannelVersion),
                msg = new
                {
                    from_user_id = string.Empty,
                    to_user_id = message.ToUserId,
                    client_id = $"cs-{Guid.NewGuid():N}",
                    message_type = 2,
                    message_state = 2,
                    context_token = message.ContextToken,
                    item_list = new object[] { item },
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
        if (ret == 0 || (ret is null && errCode is null))
        {
            return;
        }

        if (ret == ILinkSessionExpiredException.SessionExpiredCode || errCode == ILinkSessionExpiredException.SessionExpiredCode)
        {
            throw new ILinkSessionExpiredException();
        }

        var message = errorMessage;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"iLink 调用失败（ret={ret?.ToString() ?? "null"}, errcode={errCode?.ToString() ?? "null"}）。";
        }

        throw new ILinkApiException(message, errCode ?? ret);
    }

    private static string? ExtractText(IncomingRawMessage message)
    {
        foreach (var item in message.ItemList)
        {
            if (item.Type == 1 && !string.IsNullOrWhiteSpace(item.TextItem?.Text))
            {
                return item.TextItem.Text;
            }

            if (item.Type == 3 && !string.IsNullOrWhiteSpace(item.VoiceItem?.Text))
            {
                return $"[语音] {item.VoiceItem.Text}";
            }

            if (item.Type == 2)
            {
                return "[图片]";
            }

            if (item.Type == 4)
            {
                return string.IsNullOrWhiteSpace(item.FileItem?.FileName)
                    ? "[文件]"
                    : $"[文件] {item.FileItem.FileName}";
            }

            if (item.Type == 5)
            {
                return "[视频]";
            }
        }

        return message.Text;
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

        [JsonPropertyName("item_list")]
        public IReadOnlyList<MessageItem> ItemList { get; init; } = [];

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];

        public JsonElement Raw => JsonSerializer.SerializeToElement(new
        {
            from_user_id = FromUserId,
            to_user_id = ToUserId,
            context_token = ContextToken,
            text = Text,
            item_list = ItemList,
            extension_data = ExtensionData,
        });
    }

    private sealed class MessageItem
    {
        [JsonPropertyName("type")]
        public int Type { get; init; }

        [JsonPropertyName("text_item")]
        public TextItem? TextItem { get; init; }

        [JsonPropertyName("voice_item")]
        public VoiceItem? VoiceItem { get; init; }

        [JsonPropertyName("file_item")]
        public FileItem? FileItem { get; init; }
    }

    private sealed class TextItem
    {
        [JsonPropertyName("text")]
        public string? Text { get; init; }
    }

    private sealed class VoiceItem
    {
        [JsonPropertyName("text")]
        public string? Text { get; init; }
    }

    private sealed class FileItem
    {
        [JsonPropertyName("file_name")]
        public string? FileName { get; init; }
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
