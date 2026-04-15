using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using ILink4NET.Exceptions;
using ILink4NET.Models;
using ILink4NET.Options;
using ILink4NET.Stores;
using ILink4NET.Transport;

namespace ILink4NET.Typing;

/// <summary>
/// 输入状态实现。
/// </summary>
public sealed class TypingService : ITypingService
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromHours(24);

    private readonly ILinkHttpClient _httpClient;
    private readonly ILinkClientOptions _options;
    private readonly IContextTokenStore _contextTokenStore;
    private readonly ConcurrentDictionary<string, TicketCacheItem> _ticketCache = new(StringComparer.Ordinal);

    public TypingService(ILinkHttpClient httpClient, ILinkClientOptions options, IContextTokenStore contextTokenStore)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _contextTokenStore = contextTokenStore ?? throw new ArgumentNullException(nameof(contextTokenStore));
    }

    public async Task SendTypingAsync(string botToken, string userId, string contextToken, TypingIndicatorStatus status, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextToken);

        var ticket = await GetTypingTicketAsync(botToken, userId, contextToken, cancellationToken).ConfigureAwait(false);

        var response = await _httpClient.PostAsync<SimpleResponse>(
            "/sendtyping",
            new
            {
                base_info = new BaseInfo(_options.ChannelVersion),
                ilink_user_id = userId,
                typing_ticket = ticket,
                status = (int)status,
            },
            botToken,
            cancellationToken).ConfigureAwait(false);

        EnsureResponseState(response.Ret, response.ErrCode, response.ErrorMessage);
    }

    public async Task SendTypingAsync(string botToken, string userId, TypingIndicatorStatus status, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var contextToken = await _contextTokenStore.GetAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new ILinkApiException($"用户 {userId} 缺少 context_token，无法发送输入状态。");

        await SendTypingAsync(botToken, userId, contextToken, status, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetTypingTicketAsync(string botToken, string userId, string contextToken, CancellationToken cancellationToken)
    {
        if (_ticketCache.TryGetValue(userId, out var cached)
            && DateTimeOffset.UtcNow - cached.CreatedAt < TicketLifetime)
        {
            return cached.Ticket;
        }

        var response = await _httpClient.PostAsync<GetConfigResponse>(
            "/getconfig",
            new
            {
                base_info = new BaseInfo(_options.ChannelVersion),
                ilink_user_id = userId,
                context_token = contextToken,
            },
            botToken,
            cancellationToken).ConfigureAwait(false);

        EnsureResponseState(response.Ret, response.ErrCode, response.ErrorMessage);

        if (string.IsNullOrWhiteSpace(response.TypingTicket))
        {
            throw new ILinkApiException("getconfig 未返回 typing_ticket。", response.ErrCode);
        }

        _ticketCache[userId] = new TicketCacheItem(response.TypingTicket, DateTimeOffset.UtcNow);
        return response.TypingTicket;
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

    private sealed record TicketCacheItem(string Ticket, DateTimeOffset CreatedAt);

    private sealed class GetConfigResponse
    {
        [JsonPropertyName("ret")]
        public int? Ret { get; init; }

        [JsonPropertyName("errcode")]
        public int? ErrCode { get; init; }

        [JsonPropertyName("errmsg")]
        public string? ErrorMessage { get; init; }

        [JsonPropertyName("typing_ticket")]
        public string? TypingTicket { get; init; }
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
