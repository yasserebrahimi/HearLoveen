using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HearLoveen.Infrastructure.Persistence;
namespace HearLoveen.Infrastructure.Seeding;
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db){ await db.Database.MigrateAsync(); }
}
