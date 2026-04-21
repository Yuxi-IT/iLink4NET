using ILink4NET.Models;

namespace ILink4NET.Typing;

public interface ITypingService
{
    Task SendTypingAsync(string botToken, string userId, string contextToken, TypingIndicatorStatus status, CancellationToken cancellationToken = default);

    Task SendTypingAsync(string botToken, string userId, TypingIndicatorStatus status, CancellationToken cancellationToken = default);
}
