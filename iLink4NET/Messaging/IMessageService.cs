using ILink4NET.Models;

namespace ILink4NET.Messaging;

/// <summary>
/// iLink 消息收发服务。
/// </summary>
public interface IMessageService
{
    Task<UpdateBatch> GetUpdatesAsync(string botToken, string cursor, CancellationToken cancellationToken = default);

    Task SendTextMessageAsync(string botToken, OutgoingTextMessage message, CancellationToken cancellationToken = default);

    Task SendMediaMessageAsync(string botToken, OutgoingMediaMessage message, CancellationToken cancellationToken = default);

    Task SendReplyAsync(string botToken, IncomingMessage incomingMessage, string text, CancellationToken cancellationToken = default);
}
