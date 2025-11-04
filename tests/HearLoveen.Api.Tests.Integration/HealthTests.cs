using System.Net;
using System.Threading.Tasks;
using Xunit;
namespace HearLoveen.Api.Tests.Integration;
public class HealthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public HealthTests(ApiFactory factory) => _factory = factory;
    [Fact] public async Task Health_returns_ok()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
