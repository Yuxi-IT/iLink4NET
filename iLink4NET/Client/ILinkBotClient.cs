using ILink4NET.Authentication;
using ILink4NET.Media;
using ILink4NET.Messaging;
using ILink4NET.Models;
using ILink4NET.Options;
using ILink4NET.Stores;
using ILink4NET.Typing;
using ILink4NET.Transport;

namespace ILink4NET.Client;

/// <summary>
/// iLink 对外统一客户端。
/// </summary>
public sealed class ILinkBotClient
{
    private readonly ILinkClientOptions _options;

    public ILinkBotClient(
        ILinkClientOptions options,
        ILoginService loginService,
        IMessageService messageService,
        ITypingService typingService,
        IMediaService mediaService,
        IContextTokenStore contextTokenStore)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Login = loginService ?? throw new ArgumentNullException(nameof(loginService));
        Messages = messageService ?? throw new ArgumentNullException(nameof(messageService));
        Typing = typingService ?? throw new ArgumentNullException(nameof(typingService));
        Media = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        ContextTokens = contextTokenStore ?? throw new ArgumentNullException(nameof(contextTokenStore));
    }

    /// <summary>
    /// 当前会话凭证，登录成功后赋值。
    /// </summary>
    public SessionCredentials? Credentials { get; private set; }

    public ILoginService Login { get; }

    public IMessageService Messages { get; }

    public ITypingService Typing { get; }

    public IMediaService Media { get; }

    public IContextTokenStore ContextTokens { get; }

    public void SetCredentials(SessionCredentials credentials)
    {
        Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _options.ApiBaseUri = credentials.ApiBaseUri;
    }

    public async Task<SessionCredentials> LoginByQrCodeAsync(string qrCode, TimeSpan pollInterval, CancellationToken cancellationToken = default)
    {
        var credentials = await Login.WaitForConfirmationAsync(qrCode, pollInterval, cancellationToken).ConfigureAwait(false);
        SetCredentials(credentials);
        return credentials;
    }

    public async Task<UpdateBatch> GetUpdatesAsync(string cursor, CancellationToken cancellationToken = default)
    {
        var token = GetBotTokenOrThrow();
        return await Messages.GetUpdatesAsync(token, cursor, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReplyTextAsync(IncomingMessage message, string text, CancellationToken cancellationToken = default)
    {
        var token = GetBotTokenOrThrow();
        await Messages.SendReplyAsync(token, message, text, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendMediaMessageAsync(
        string toUserId,
        string contextToken,
        UploadedMediaReference mediaReference,
        CancellationToken cancellationToken = default)
    {
        var token = GetBotTokenOrThrow();
        await Messages.SendMediaMessageAsync(
            token,
            new OutgoingMediaMessage(toUserId, contextToken, mediaReference),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SendTypingAsync(string userId, TypingIndicatorStatus status, CancellationToken cancellationToken = default)
    {
        var token = GetBotTokenOrThrow();
        await Typing.SendTypingAsync(token, userId, status, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UploadedMediaReference> UploadMediaAsync(
        string fileKey,
        MediaType mediaType,
        byte[] rawBytes,
        CancellationToken cancellationToken = default)
    {
        var token = GetBotTokenOrThrow();

        var encrypted = Media.EncryptMedia(rawBytes);
        var ticket = await Media.RequestUploadTicketAsync(token, fileKey, mediaType, encrypted, cancellationToken).ConfigureAwait(false);
        return await Media.UploadToCdnAsync(ticket.UploadParam, encrypted.AesKeyBase64, encrypted.EncryptedBytes, cancellationToken).ConfigureAwait(false);
    }

    private string GetBotTokenOrThrow()
    {
        return Credentials?.BotToken
            ?? throw new InvalidOperationException("当前客户端未登录，请先完成二维码登录并设置凭证。");
    }

    public static ILinkBotClient CreateDefault(HttpClient httpClient, ILinkClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        var finalOptions = options ?? new ILinkClientOptions();
        var contextStore = new InMemoryContextTokenStore();
        var protocolClient = new ILinkHttpClient(httpClient, finalOptions);

        return new ILinkBotClient(
            finalOptions,
            new LoginService(protocolClient),
            new MessageService(protocolClient, contextStore, finalOptions),
            new TypingService(protocolClient, finalOptions, contextStore),
            new MediaService(protocolClient, httpClient, finalOptions),
            contextStore);
    }
}
