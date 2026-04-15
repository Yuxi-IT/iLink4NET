using System.Text.Json;

namespace ILink4NET.Models;

/// <summary>
/// 入站消息。
/// </summary>
public sealed record IncomingMessage(
    string UserId,
    string ContextToken,
    string? Text,
    JsonElement Raw);
