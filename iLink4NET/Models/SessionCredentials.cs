namespace ILink4NET.Models;

public sealed record SessionCredentials(
    string BotToken,
    string ILinkBotId,
    string ILinkUserId,
    Uri ApiBaseUri);
