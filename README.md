# iLink4NET

面向 .NET 10 的微信 iLink Bot 协议客户端库。

## 快速开始

```csharp
using ILink4NET.Client;

var httpClient = new HttpClient();
var botClient = ILinkBotClient.CreateDefault(httpClient);

var qrCode = await botClient.Login.CreateQrCodeAsync();
Console.WriteLine($"请扫码：{qrCode.QrCodeImageUri}");

await botClient.LoginByQrCodeAsync(qrCode.QrCode, TimeSpan.FromSeconds(2));

var cursor = string.Empty;
while (true)
{
    var batch = await botClient.GetUpdatesAsync(cursor);
    cursor = batch.NextCursor;

    foreach (var message in batch.Messages)
    {
        await botClient.SendTypingAsync(message.UserId, TypingIndicatorStatus.Start);
        await botClient.ReplyTextAsync(message, $"Echo: {message.Text}");
        await botClient.SendTypingAsync(message.UserId, TypingIndicatorStatus.Stop);
    }
}
```

## 功能覆盖

- 二维码登录（创建二维码、轮询确认）
- 长轮询消息接收与文本/媒体发送
- `context_token` 自动缓存与回复复用
- 输入状态（`typing_ticket` 缓存）
- 媒体 AES-128-ECB 加解密、CDN 上传下载

## 鸣谢
[corespeed-io/WechatBot](https://github.com/corespeed-io/wechatbot)为本项目提供了很多参考