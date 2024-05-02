using Microsoft.AspNetCore.Identity;

namespace usercenter_backend.Model
{
    public class User : IdentityUser<long>
    {
        public string? AvatarUrl { get; set; }
        public int Gender { get; set; }
        public int UserStatus { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public bool IsDelete { get; set; }

    }
}
