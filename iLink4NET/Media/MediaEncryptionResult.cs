namespace ILink4NET.Media;

/// <summary>
/// 加密后的媒体准备结果。
/// </summary>
public sealed record MediaEncryptionResult(
    byte[] EncryptedBytes,
    string AesKeyBase64,
    string AesKeyHex,
    int RawSize,
    int EncryptedSize);
