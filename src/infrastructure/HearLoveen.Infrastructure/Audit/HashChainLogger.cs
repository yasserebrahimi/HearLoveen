
using System.Security.Cryptography;
using System.Text;
namespace HearLoveen.Infrastructure.Audit;
public class HashChainLogger
{
    private byte[] _last = new byte[32];
    public string Append(string jsonEvent)
    {
        var data = Encoding.UTF8.GetBytes(jsonEvent);
        var combined = new byte[_last.Length + data.Length];
        _last.CopyTo(combined, 0);
        data.CopyTo(combined, _last.Length);
        _last = SHA256.HashData(combined);
        return Convert.ToBase64String(_last);
    }
}
