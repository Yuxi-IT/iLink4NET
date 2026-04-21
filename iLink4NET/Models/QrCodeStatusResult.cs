namespace ILink4NET.Models;

public sealed record QrCodeStatusResult(
    QrCodeLoginStatus Status,
    SessionCredentials? Credentials);
