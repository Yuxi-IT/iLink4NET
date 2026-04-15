namespace ILink4NET.Models;

/// <summary>
/// 二维码轮询结果。
/// </summary>
public sealed record QrCodeStatusResult(
    QrCodeLoginStatus Status,
    SessionCredentials? Credentials);
