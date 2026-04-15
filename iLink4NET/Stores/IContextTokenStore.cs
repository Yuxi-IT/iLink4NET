namespace ILink4NET.Stores;

/// <summary>
/// 用户上下文 Token 存储接口。
/// </summary>
public interface IContextTokenStore
{
    ValueTask SetAsync(string userId, string contextToken, CancellationToken cancellationToken = default);

    ValueTask<string?> GetAsync(string userId, CancellationToken cancellationToken = default);

    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
