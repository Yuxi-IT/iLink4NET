namespace ILink4NET.Exceptions;

/// <summary>
/// iLink 协议异常。
/// </summary>
public class ILinkApiException : Exception
{
    public ILinkApiException(string message, int? errCode = null)
        : base(message)
    {
        ErrCode = errCode;
    }

    public int? ErrCode { get; }
}
