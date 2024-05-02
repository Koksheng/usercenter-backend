using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

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

        // This property will not be mapped to the database
        [NotMapped]
        public bool IsAdmin { get; set; }

    }
}
