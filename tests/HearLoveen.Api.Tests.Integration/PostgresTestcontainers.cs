using System.Threading.Tasks;
using Xunit;
namespace HearLoveen.Api.Tests.Integration;
public class PostgresTestcontainers
{
    [Fact(Skip="Enable after adding Testcontainers package")]
    public async Task Can_start_postgres_and_migrate(){ await Task.CompletedTask; }
}
