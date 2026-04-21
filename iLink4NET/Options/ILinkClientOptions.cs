namespace ILink4NET.Options;

public sealed class ILinkClientOptions
{
    public Uri ApiBaseUri { get; set; } = new("https://ilinkai.weixin.qq.com");

    public Uri CdnBaseUri { get; set; } = new("https://novac2c.cdn.weixin.qq.com/c2c/");

    public string ChannelVersion { get; set; } = "1.0.2";
}
