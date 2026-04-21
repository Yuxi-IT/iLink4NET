namespace ILink4NET.Exceptions;

public class ILinkApiException : Exception
{
    public ILinkApiException(string message, int? errCode = null)
        : base(message)
    {
        ErrCode = errCode;
    }

    public int? ErrCode { get; }
}
