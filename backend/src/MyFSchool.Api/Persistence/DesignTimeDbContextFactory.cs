using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Api.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MyFSchoolDbContext>
{
    public MyFSchoolDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing required setting: ConnectionStrings__Default");
        }

        var options = new DbContextOptionsBuilder<MyFSchoolDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new MyFSchoolDbContext(options);
    }
}
