namespace ILink4NET.Options;

/// <summary>
/// iLink 客户端配置。
/// </summary>
public sealed class ILinkClientOptions
{
    /// <summary>
    /// iLink API 基础地址。
    /// </summary>
    public Uri ApiBaseUri { get; set; } = new("https://ilinkai.weixin.qq.com");

    /// <summary>
    /// 微信 CDN 地址。
    /// </summary>
    public Uri CdnBaseUri { get; set; } = new("https://novac2c.cdn.weixin.qq.com/c2c");

    /// <summary>
    /// 协议要求固定为 2.0.0。
    /// </summary>
    public string ChannelVersion { get; set; } = "2.0.0";
}
