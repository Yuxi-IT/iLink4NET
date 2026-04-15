using System.Text.Json.Serialization;
using ILink4NET.Exceptions;
using ILink4NET.Models;
using ILink4NET.Transport;

namespace ILink4NET.Authentication;

/// <summary>
/// iLink 扫码登录实现。
/// </summary>
public sealed class LoginService : ILoginService
{
    private readonly ILinkHttpClient _httpClient;

    public LoginService(ILinkHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<LoginQrCode> CreateQrCodeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync<GetQrCodeResponse>("/get_bot_qrcode?bot_type=3", cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(response.QrCode) || string.IsNullOrWhiteSpace(response.QrCodeImageContent))
        {
            throw new ILinkApiException("二维码响应缺少必要字段。", response.ErrCode);
        }

        return new LoginQrCode(response.QrCode, new Uri(response.QrCodeImageContent));
    }

    public async Task<QrCodeStatusResult> QueryQrCodeStatusAsync(string qrCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qrCode);

        var response = await _httpClient.GetAsync<GetQrCodeStatusResponse>($"/get_qrcode_status?qrcode={Uri.EscapeDataString(qrCode)}", cancellationToken).ConfigureAwait(false);

        var status = MapStatus(response.Status);
        var credentials = status == QrCodeLoginStatus.Confirmed
            ? CreateCredentials(response)
            : null;

        return new QrCodeStatusResult(status, credentials);
    }

    public async Task<SessionCredentials> WaitForConfirmationAsync(string qrCode, TimeSpan pollInterval, CancellationToken cancellationToken = default)
    {
        if (pollInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollInterval), "轮询间隔必须大于 0。");
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await QueryQrCodeStatusAsync(qrCode, cancellationToken).ConfigureAwait(false);
            switch (result.Status)
            {
                case QrCodeLoginStatus.Confirmed when result.Credentials is not null:
                    return result.Credentials;
                case QrCodeLoginStatus.Expired:
                    throw new ILinkApiException("二维码已过期，请重新获取。");
                default:
                    await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }

    private static SessionCredentials CreateCredentials(GetQrCodeStatusResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.BotToken)
            || string.IsNullOrWhiteSpace(response.ILinkBotId)
            || string.IsNullOrWhiteSpace(response.ILinkUserId)
            || string.IsNullOrWhiteSpace(response.BaseUrl))
        {
            throw new ILinkApiException("登录已确认，但返回凭证字段不完整。", response.ErrCode);
        }

        return new SessionCredentials(
            response.BotToken,
            response.ILinkBotId,
            response.ILinkUserId,
            new Uri(response.BaseUrl));
    }

    private static QrCodeLoginStatus MapStatus(string? rawStatus)
    {
        return rawStatus?.ToLowerInvariant() switch
        {
            "wait" => QrCodeLoginStatus.Wait,
            "scaned" => QrCodeLoginStatus.Scanned,
            "confirmed" => QrCodeLoginStatus.Confirmed,
            "expired" => QrCodeLoginStatus.Expired,
            _ => QrCodeLoginStatus.Unknown,
        };
    }

    private sealed class GetQrCodeResponse
    {
        [JsonPropertyName("qrcode")]
        public string? QrCode { get; init; }

        [JsonPropertyName("qrcode_img_content")]
        public string? QrCodeImageContent { get; init; }

        [JsonPropertyName("errcode")]
        public int? ErrCode { get; init; }
    }

    private sealed class GetQrCodeStatusResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("bot_token")]
        public string? BotToken { get; init; }

        [JsonPropertyName("ilink_bot_id")]
        public string? ILinkBotId { get; init; }

        [JsonPropertyName("ilink_user_id")]
        public string? ILinkUserId { get; init; }

        [JsonPropertyName("baseurl")]
        public string? BaseUrl { get; init; }

        [JsonPropertyName("errcode")]
        public int? ErrCode { get; init; }
    }
}
