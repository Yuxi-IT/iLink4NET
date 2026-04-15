namespace ILink4NET.Exceptions;

/// <summary>
/// 会话过期异常（errcode = -14）。
/// </summary>
public sealed class ILinkSessionExpiredException : ILinkApiException
{
    public const int SessionExpiredCode = -14;

    public ILinkSessionExpiredException(string message = "会话已过期，请重新扫码登录。")
        : base(message, SessionExpiredCode)
    {
    }
}
