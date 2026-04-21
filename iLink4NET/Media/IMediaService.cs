namespace ILink4NET.Media;

public interface IMediaService
{
    MediaEncryptionResult EncryptMedia(byte[] rawBytes);

    byte[] DecryptMedia(byte[] encryptedBytes, string aesKey);

    Task<MediaUploadTicket> RequestUploadTicketAsync(
        string botToken,
        string fileKey,
        string toUserId,
        MediaType mediaType,
        MediaEncryptionResult encryptedMedia,
        CancellationToken cancellationToken = default);

    Task<UploadedMediaReference> UploadToCdnAsync(
        string uploadParam,
        string fileKey,
        string aesKey,
        byte[] encryptedBytes,
        CancellationToken cancellationToken = default);

    Task<byte[]> DownloadAndDecryptAsync(string encryptQueryParam, string aesKey, CancellationToken cancellationToken = default);
}
