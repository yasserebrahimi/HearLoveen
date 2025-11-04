namespace HearLoveen.Api.Features;
public interface IFeatureFlags { bool Enabled(string key); }
public class ConfigFeatureFlags : IFeatureFlags
{
    private readonly IConfiguration _cfg;
    public ConfigFeatureFlags(IConfiguration cfg) => _cfg = cfg;
    public bool Enabled(string key) => _cfg.GetValue<bool>($"Features:{key}");
}
