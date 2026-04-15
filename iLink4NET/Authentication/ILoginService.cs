using ILink4NET.Models;

namespace ILink4NET.Authentication;

/// <summary>
/// 扫码登录服务。
/// </summary>
public interface ILoginService
{
    Task<LoginQrCode> CreateQrCodeAsync(CancellationToken cancellationToken = default);

    Task<QrCodeStatusResult> QueryQrCodeStatusAsync(string qrCode, CancellationToken cancellationToken = default);

    Task<SessionCredentials> WaitForConfirmationAsync(string qrCode, TimeSpan pollInterval, CancellationToken cancellationToken = default);
}
