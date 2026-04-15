namespace ILink4NET.Models;

/// <summary>
/// 登录成功后可持久化的凭证信息。
/// </summary>
public sealed record SessionCredentials(
    string BotToken,
    string ILinkBotId,
    string ILinkUserId,
    Uri ApiBaseUri);
