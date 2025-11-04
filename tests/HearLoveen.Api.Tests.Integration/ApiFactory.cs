using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
namespace HearLoveen.Api.Tests.Integration;
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services => {
            // TODO: replace DB with InMemory/Testcontainers & seed
        });
        return base.CreateHost(builder);
    }
}
