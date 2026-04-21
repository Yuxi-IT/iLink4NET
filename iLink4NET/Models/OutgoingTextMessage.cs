namespace ILink4NET.Models;

public sealed record OutgoingTextMessage(
    string ToUserId,
    string ContextToken,
    string Text);
