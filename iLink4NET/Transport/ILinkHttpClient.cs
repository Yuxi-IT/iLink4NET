using System.Text;
using System.Text.Json;
using ILink4NET.Exceptions;
using ILink4NET.Models;
using ILink4NET.Options;

namespace ILink4NET.Transport;

/// <summary>
/// iLink HTTP 协议客户端，负责组装通用请求头与基础请求体。
/// </summary>
public sealed class ILinkHttpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ILinkClientOptions _options;

    public ILinkHttpClient(HttpClient httpClient, ILinkClientOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<T> GetAsync<T>(string pathAndQuery, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathAndQuery);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_options.ApiBaseUri, pathAndQuery));
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ParseResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> PostAsync<T>(string path, object payload, string? botToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.ApiBaseUri, path));
        request.Content = BuildJsonContent(payload);

        if (!string.IsNullOrWhiteSpace(botToken))
        {
            request.Headers.Add("AuthorizationType", "ilink_bot_token");
            request.Headers.Add("Authorization", $"Bearer {botToken}");
            request.Headers.Add("X-WECHAT-UIN", WeChatUinGenerator.GenerateBase64Uin());
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ParseResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    private static StringContent BuildJsonContent(object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task<T> ParseResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new ILinkApiException($"HTTP 调用失败，状态码 {(int)response.StatusCode}，响应：{content}");
        }

        var value = JsonSerializer.Deserialize<T>(content, JsonOptions);
        if (value is null)
        {
            throw new ILinkApiException("响应体为空或反序列化失败。");
        }

        return value;
    }
}
