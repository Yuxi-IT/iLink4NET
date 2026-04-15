namespace ILink4NET.Models;

/// <summary>
/// 二维码登录状态。
/// </summary>
public enum QrCodeLoginStatus
{
    Wait,
    Scanned,
    Confirmed,
    Expired,
    Unknown,
}
