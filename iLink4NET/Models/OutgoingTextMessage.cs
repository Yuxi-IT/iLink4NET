namespace ILink4NET.Models;

/// <summary>
/// 文本消息发送请求。
/// </summary>
public sealed record OutgoingTextMessage(
    string ToUserId,
    string ContextToken,
    string Text);
