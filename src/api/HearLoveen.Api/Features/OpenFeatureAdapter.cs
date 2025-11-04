namespace HearLoveen.Api.Features;
public interface IFeatureFlags { bool Enabled(string key); string Variant(string key, string defaultVariant = "A"); }
public class ConfigFeatureFlags : IFeatureFlags
{
    private readonly IConfiguration _cfg;
    public ConfigFeatureFlags(IConfiguration cfg) => _cfg = cfg;
    public bool Enabled(string key) => _cfg.GetValue<bool>($"Features:{key}");
    public string Variant(string key, string def = "A") => _cfg.GetValue<string>($"Features:{key}:Variant") ?? def;
}
public class OpenFeatureFlags : IFeatureFlags
{
    public bool Enabled(string key) { return false; }
    public string Variant(string key, string defaultVariant = "A") { return defaultVariant; }
}
