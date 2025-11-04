using System.Threading.Tasks;
using Xunit;
namespace HearLoveen.Contracts.Consumer;
public class ApiConsumerPact
{
    [Fact(Skip="Enable after adding PactNet")] public async Task Gets_health_ok(){ await Task.CompletedTask; }
}
