using ILink4NET.Media;

namespace ILink4NET.Models;

public sealed record OutgoingMediaMessage(
    string ToUserId,
    string ContextToken,
    UploadedMediaReference MediaReference,
    MediaType MediaType = MediaType.Image);
