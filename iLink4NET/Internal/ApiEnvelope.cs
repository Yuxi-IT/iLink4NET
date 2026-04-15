using System.Text.Json.Serialization;

namespace ILink4NET.Internal;

internal sealed class ApiEnvelope<T>
{
    [JsonPropertyName("ret")]
    public int? Ret { get; init; }

    [JsonPropertyName("errcode")]
    public int? ErrCode { get; init; }

    [JsonPropertyName("errmsg")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    public T? Payload => Data;
}
