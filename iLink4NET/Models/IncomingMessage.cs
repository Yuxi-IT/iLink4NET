using System.Text.Json;

namespace ILink4NET.Models;

public sealed record IncomingMessage(
    string UserId,
    string ContextToken,
    string? Text,
    JsonElement Raw);
