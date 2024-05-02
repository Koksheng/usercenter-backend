using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using usercenter_backend.Model;

namespace usercenter_backend
{
    public class MyDbContext : IdentityDbContext<User, Role, long>
    {
        public MyDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}
