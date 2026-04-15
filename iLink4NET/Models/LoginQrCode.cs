namespace ILink4NET.Models;

/// <summary>
/// 二维码申请结果。
/// </summary>
public sealed record LoginQrCode(
    string QrCode,
    Uri QrCodeImageUri);
