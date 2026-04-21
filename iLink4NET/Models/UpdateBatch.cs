namespace ILink4NET.Models;

public sealed record UpdateBatch(
    string NextCursor,
    IReadOnlyList<IncomingMessage> Messages);
