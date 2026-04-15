using System.Security.Cryptography;
using System.Text;

namespace ILink4NET.Transport;

internal static class WeChatUinGenerator
{
    public static string GenerateBase64Uin()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);

        var uintValue = BitConverter.ToUInt32(bytes);
        var decimalString = uintValue.ToString();
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(decimalString));
    }
}
