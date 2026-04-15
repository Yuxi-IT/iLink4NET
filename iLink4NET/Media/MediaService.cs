using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using ILink4NET.Exceptions;
using ILink4NET.Models;
using ILink4NET.Options;
using ILink4NET.Transport;

namespace ILink4NET.Media;

/// <summary>
/// 媒体上传/下载实现。
/// </summary>
public sealed class MediaService : IMediaService
{
    private readonly ILinkHttpClient _apiClient;
    private readonly HttpClient _httpClient;
    private readonly ILinkClientOptions _options;

    public MediaService(ILinkHttpClient apiClient, HttpClient httpClient, ILinkClientOptions options)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public MediaEncryptionResult EncryptMedia(byte[] rawBytes)
    {
        ArgumentNullException.ThrowIfNull(rawBytes);

        var aesKey = RandomNumberGenerator.GetBytes(16);
        var encrypted = TransformAesEcb(rawBytes, aesKey, encrypt: true);

        return new MediaEncryptionResult(
            encrypted,
            Convert.ToBase64String(aesKey),
            Convert.ToHexString(aesKey).ToLowerInvariant(),
            rawBytes.Length,
            encrypted.Length);
    }

    public byte[] DecryptMedia(byte[] encryptedBytes, string aesKey)
    {
        ArgumentNullException.ThrowIfNull(encryptedBytes);
        var key = DecodeAesKey(aesKey);
        return TransformAesEcb(encryptedBytes, key, encrypt: false);
    }

    public async Task<MediaUploadTicket> RequestUploadTicketAsync(
        string botToken,
        string fileKey,
        MediaType mediaType,
        MediaEncryptionResult encryptedMedia,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileKey);
        ArgumentNullException.ThrowIfNull(encryptedMedia);

        var response = await _apiClient.PostAsync<GetUploadUrlResponse>(
            "/getuploadurl",
            new
            {
                base_info = new BaseInfo(_options.ChannelVersion),
                filekey = fileKey,
                media_type = ToMediaTypeValue(mediaType),
                rawsize = encryptedMedia.RawSize,
                filesize = encryptedMedia.EncryptedSize,
                aeskey = encryptedMedia.AesKeyBase64,
            },
            botToken,
            cancellationToken).ConfigureAwait(false);

        EnsureResponseState(response.Ret, response.ErrCode, response.ErrorMessage);

        if (string.IsNullOrWhiteSpace(response.UploadParam))
        {
            throw new ILinkApiException("getuploadurl 未返回 upload_param。", response.ErrCode);
        }

        return new MediaUploadTicket(response.UploadParam);
    }

    public async Task<UploadedMediaReference> UploadToCdnAsync(
        string uploadParam,
        string aesKey,
        byte[] encryptedBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadParam);
        ArgumentException.ThrowIfNullOrWhiteSpace(aesKey);
        ArgumentNullException.ThrowIfNull(encryptedBytes);

        var uploadUri = new Uri(_options.CdnBaseUri, $"/upload?encrypted_query_param={Uri.EscapeDataString(uploadParam)}");

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUri)
        {
            Content = new ByteArrayContent(encryptedBytes),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new ILinkApiException($"CDN 上传失败，状态码 {(int)response.StatusCode}。");
        }

        if (!response.Headers.TryGetValues("x-encrypted-param", out var values))
        {
            throw new ILinkApiException("CDN 上传成功但未返回 x-encrypted-param。", null);
        }

        var encryptedParam = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(encryptedParam))
        {
            throw new ILinkApiException("x-encrypted-param 为空。", null);
        }

        return new UploadedMediaReference(encryptedParam, aesKey);
    }

    public async Task<byte[]> DownloadAndDecryptAsync(string encryptQueryParam, string aesKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptQueryParam);
        ArgumentException.ThrowIfNullOrWhiteSpace(aesKey);

        var downloadUri = new Uri(_options.CdnBaseUri, $"/download?encrypted_query_param={Uri.EscapeDataString(encryptQueryParam)}");
        using var response = await _httpClient.GetAsync(downloadUri, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new ILinkApiException($"CDN 下载失败，状态码 {(int)response.StatusCode}。");
        }

        var encryptedBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        return DecryptMedia(encryptedBytes, aesKey);
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

    private static string ToMediaTypeValue(MediaType mediaType)
    {
        return mediaType switch
        {
            MediaType.Image => "image",
            MediaType.Video => "video",
            MediaType.File => "file",
            MediaType.Voice => "voice",
            _ => throw new ArgumentOutOfRangeException(nameof(mediaType), mediaType, "不支持的媒体类型。"),
        };
    }

    private static byte[] TransformAesEcb(byte[] bytes, byte[] aesKey, bool encrypt)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = aesKey;

        using var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
        return transform.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    private static byte[] DecodeAesKey(string aesKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aesKey);

        if (aesKey.Length == 32 && IsHexString(aesKey))
        {
            return Convert.FromHexString(aesKey);
        }

        var decoded = Convert.FromBase64String(aesKey);
        if (decoded.Length == 16)
        {
            return decoded;
        }

        var decodedString = Encoding.UTF8.GetString(decoded);
        if (decodedString.Length == 32 && IsHexString(decodedString))
        {
            return Convert.FromHexString(decodedString);
        }

        throw new ILinkApiException("不支持的 AES Key 格式。", null);
    }

    private static bool IsHexString(string value)
    {
        foreach (var ch in value)
        {
            if (!Uri.IsHexDigit(ch))
            {
                return false;
            }
        }

        return true;
    }

    private sealed class GetUploadUrlResponse
    {
        [JsonPropertyName("ret")]
        public int? Ret { get; init; }

        [JsonPropertyName("errcode")]
        public int? ErrCode { get; init; }

        [JsonPropertyName("errmsg")]
        public string? ErrorMessage { get; init; }

        [JsonPropertyName("upload_param")]
        public string? UploadParam { get; init; }
    }
}
