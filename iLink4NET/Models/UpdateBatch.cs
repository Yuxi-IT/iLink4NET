namespace ILink4NET.Models;

/// <summary>
/// 一次长轮询返回的消息批次。
/// </summary>
public sealed record UpdateBatch(
    string NextCursor,
    IReadOnlyList<IncomingMessage> Messages);
