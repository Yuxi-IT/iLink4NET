namespace ILink4NET.Media;

/// <summary>
/// 发送媒体消息所需字段。
/// </summary>
public sealed record UploadedMediaReference(
    string EncryptQueryParam,
    string AesKey);
