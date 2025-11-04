using System.Security.Cryptography;
using System.Text;
namespace HearLoveen.Api.Experiments;
public static class Bucketer
{
    public static int Bucket(Guid userId, string experimentKey)
    {
        var data = Encoding.UTF8.GetBytes($"{userId}:{experimentKey}");
        var hash = SHA256.HashData(data);
        uint v = BitConverter.ToUInt32(hash, 0);
        return (int)(v % 100);
    }
    public static string Variant(int bucket, params (string name, int weight)[] splits)
    {
        int cum = 0;
        foreach (var s in splits){ cum += s.weight; if (bucket < cum) return s.name; }
        return splits.Length > 0 ? splits[^1].name : "A";
    }
}
public static class Cuped
{
    public static double Adjust(double y, double x, double expectedX, double theta) => y - theta * (x - expectedX);
    public static double Theta(double[] X, double[] Y)
    {
        if (X.Length != Y.Length || X.Length == 0) return 0;
        double meanX = X.Average(), meanY = Y.Average();
        double cov=0, var=0;
        for (int i=0;i<X.Length;i++){ cov += (X[i]-meanX)*(Y[i]-meanY); var += (X[i]-meanX)*(X[i]-meanX); }
        if (var == 0) return 0; return cov/var;
    }
}
