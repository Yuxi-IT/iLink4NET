namespace ILink4NET.Media;

public sealed record UploadedMediaReference(
    string EncryptQueryParam,
    string AesKey,
    string? FileName = null,
    long? FileSize = null);
