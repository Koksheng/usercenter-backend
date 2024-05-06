using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using usercenter_backend;

namespace IdentityFramework
{
    public class DbContextDesignTimeFactory : IDesignTimeDbContextFactory<MyDbContext>
    {
        public MyDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<MyDbContext> builder = new DbContextOptionsBuilder<MyDbContext>();
            builder.UseSqlServer("Server=.;Database=usercenter;Trusted_Connection=True;TrustServerCertificate=True;");
            //builder.UseSqlServer("Server=(localdb)\\Local;Database=usercenter;Trusted_Connection=True;TrustServerCertificate=True;");
            return new MyDbContext(builder.Options);
        }
    }
}
