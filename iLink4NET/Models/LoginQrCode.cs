namespace ILink4NET.Models;

public sealed record LoginQrCode(
    string QrCode,
    Uri QrCodeImageUri);
