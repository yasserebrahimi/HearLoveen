using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace HearLoveen.Infrastructure.Security;
public static class CryptoService
{
    public static string? Encrypt(string? plaintext, byte[] key)
    {
        if (plaintext == null) return null;
        using var aes = new AesGcm(key);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var ct = new byte[plain.Length];
        var tag = new byte[16];
        aes.Encrypt(nonce, plain, ct, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(ct).ToArray());
    }
    public static string? Decrypt(string? cipherB64, byte[] key)
    {
        if (cipherB64 == null) return null;
        var data = Convert.FromBase64String(cipherB64);
        var nonce = data[..12];
        var tag = data[12..28];
        var ct = data[28..];
        using var aes = new AesGcm(key);
        var plain = new byte[ct.Length];
        aes.Decrypt(nonce, ct, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
    public static ValueConverter<string,string> MakeConverter(byte[] key) =>
        new ValueConverter<string,string>(v => Encrypt(v, key) ?? string.Empty, v => Decrypt(v, key) ?? string.Empty);
}
