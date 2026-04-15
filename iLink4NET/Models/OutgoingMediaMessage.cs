using ILink4NET.Media;

namespace ILink4NET.Models;

/// <summary>
/// 媒体消息发送请求。
/// </summary>
public sealed record OutgoingMediaMessage(
    string ToUserId,
    string ContextToken,
    UploadedMediaReference MediaReference);
