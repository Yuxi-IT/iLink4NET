namespace ILink4NET.Media;

public sealed record MediaEncryptionResult(
    byte[] EncryptedBytes,
    string AesKeyBase64,
    string AesKeyHexBase64,
    string AesKeyHex,
    string RawFileMd5,
    int RawSize,
    int EncryptedSize);
