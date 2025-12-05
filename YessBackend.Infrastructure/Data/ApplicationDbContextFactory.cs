using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YessBackend.Infrastructure.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Строка подключения только для миграций.
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5432;Database=yessdb;Username=yess_user;Password=secure_password"
            );

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
