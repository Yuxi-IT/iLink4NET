using System.Collections.Concurrent;

namespace ILink4NET.Stores;

/// <summary>
/// 默认内存版 Context Token 存储。
/// </summary>
public sealed class InMemoryContextTokenStore : IContextTokenStore
{
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.Ordinal);

    public ValueTask SetAsync(string userId, string contextToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextToken);
        _tokens[userId] = contextToken;
        return ValueTask.CompletedTask;
    }

    public ValueTask<string?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        _tokens.TryGetValue(userId, out var value);
        return ValueTask.FromResult(value);
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        _tokens.Clear();
        return ValueTask.CompletedTask;
    }
}
