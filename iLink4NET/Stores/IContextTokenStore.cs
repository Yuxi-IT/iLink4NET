namespace ILink4NET.Stores;

public interface IContextTokenStore
{
    ValueTask SetAsync(string userId, string contextToken, CancellationToken cancellationToken = default);

    ValueTask<string?> GetAsync(string userId, CancellationToken cancellationToken = default);

    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
