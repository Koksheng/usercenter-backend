using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;
using usercenter_backend.Model;
using usercenter_backend.Model.Request;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace IdentityFramework.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> userManager;
        private readonly RoleManager<Role> roleManager;
        private readonly IWebHostEnvironment hostEnvironment;
        private readonly ILogger<UserController> logger;

        public UserController(UserManager<User> userManager, RoleManager<Role> roleManager, IWebHostEnvironment hostEnvironment, ILogger<UserController> logger = null)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.hostEnvironment = hostEnvironment;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<string>> Test1()
        {
            if (await roleManager.RoleExistsAsync("admin") == false)
            {
                Role role = new Role { Name = "admin" };
                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded) return BadRequest("roleManager CreateAsync failed");
            }

            User user1 = await userManager.FindByNameAsync("yzk");
            if (user1 == null)
            {
                user1 = new User { UserName = "yzk" };
                var result = await userManager.CreateAsync(user1, "123456");
                if (!result.Succeeded) return BadRequest("userManager CreateAsync failed");
            }
            if (!await userManager.IsInRoleAsync(user1, "admin"))
            {
                var result = await userManager.AddToRoleAsync(user1, "admin");
                if (!result.Succeeded) return BadRequest("userManager.AddToRoleAsync failed");
            }
            return "ok";
        }

        [HttpPost]
        public async Task<ActionResult> CheckPwd(CheckPwdRequest req)
        {
            string userName = req.UserName;
            string pwd = req.Password;
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
            {
                if (hostEnvironment.IsDevelopment())
                {
                    return BadRequest("用户名不存在");
                }
                else
                {
                    return BadRequest();
                }
            }
            if (await userManager.IsLockedOutAsync(user))
            {
                return BadRequest($"{userName} is locked out. Lock out End at {user.LockoutEnd}");
            }
            if (await userManager.CheckPasswordAsync(user, pwd))
            {
                await userManager.ResetAccessFailedCountAsync(user);
                return Ok("登录成功");
            }
            else
            {
                await userManager.AccessFailedAsync(user);
                return BadRequest("用户名或者密码错误");
            }
        }

        [HttpPost]
        public async Task<ActionResult> SendResetPasswordToken(string userName)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return BadRequest("用户名不存在");
            }
            string token = await userManager.GeneratePasswordResetTokenAsync(user);
            Console.WriteLine($"验证码是{token}");
            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult> ResetPassword(string userName, string token, string newPassword)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return BadRequest("用户名不存在");
            }
            var result = await userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                await userManager.ResetAccessFailedCountAsync(user);
                return Ok("密码重置成功");
            }
            else
            {
                await userManager.AccessFailedAsync(user);
                return Ok("密码重置失败");
            }
        }


        /**
         * 
         * 
         */
        /**/
        [HttpPost]
        public async Task<long> userRegister(UserRegisterRequest userRegisterRequest)
        {
            // userAccount = UserName in DB
            if (userRegisterRequest == null)
            {
                return -1;
            }
            string userAccount = userRegisterRequest.userAccount;
            string userPassword = userRegisterRequest.userPassword;
            string checkPassword = userRegisterRequest.checkPassword;

            // 1. Verify
            if (string.IsNullOrWhiteSpace(userAccount) || string.IsNullOrWhiteSpace(userPassword) || string.IsNullOrWhiteSpace(checkPassword))
            {
                return -1;
            }
            if (userAccount.Length < 4)
            {
                return -1;
            }
            if (userPassword.Length < 8 || checkPassword.Length < 8)
                return -1;

            // userAccount cant contain special character
            string pattern = @"[^a-zA-Z0-9\s]";
            if (Regex.IsMatch(userAccount, pattern))
            {
                return -1;
            }
            // userPassword & checkPassword must same
            if (!userPassword.Equals(checkPassword))
            {
                return -1;
            }

            // userAccount cant existed
            var user = await userManager.FindByNameAsync(userAccount);
            if (user != null)
            {
                if (user.IsDelete == false)
                    return -1;
            }

            // 2. 加密 (.net core IdentityUser will encrypt themself

            // 3. Insert User to DB
            user = new User { UserName = userAccount };
            var result = await userManager.CreateAsync(user, userPassword);
            if (!result.Succeeded)
                return -1;

            return user.Id;
        }

        private const string USER_LOGIN_STATE = "userLoginState";

        [HttpPost]
        public async Task<User?> userLogin(UserLoginResponse userLoginRequest)
        {
            if (userLoginRequest == null)
            {
                return null;
            }
            string userAccount = userLoginRequest.userAccount;
            string userPassword = userLoginRequest.userPassword;

            logger.LogWarning($"{userAccount} trying to userLogin with password: {userPassword}");

            // 1. Verify
            if (string.IsNullOrWhiteSpace(userAccount) || string.IsNullOrWhiteSpace(userPassword))
            {
                return null;
            }
            if (userAccount.Length < 4)
            {
                return null;
            }
            if (userPassword.Length < 8)
                return null;
            // userAccount cant contain special character
            string pattern = @"[^a-zA-Z0-9\s]";
            if (Regex.IsMatch(userAccount, pattern))
            {
                return null;
            }

            // 2. check user is exist
            var user = await userManager.FindByNameAsync(userAccount);
            if (user == null)
            {
                logger.LogWarning($"user login failed, userAccount={userAccount} cannot be found");
                return null;
            }
            if (user.IsDelete == true)
            {
                return null;
            }
            if (await userManager.IsLockedOutAsync(user))
            {
                return null;
            }
            if (!await userManager.CheckPasswordAsync(user, userPassword))
            {
                logger.LogWarning($"user login failed, userAccount={userAccount} cannot matchh userPassword={userPassword}");
                await userManager.AccessFailedAsync(user);
                return null;
            }

            // 登录成功
            await userManager.ResetAccessFailedCountAsync(user);

            // 3. 用户脱敏 desensitization

            //User safetyUser = new User();
            //safetyUser.Id = user.Id;
            //safetyUser.UserName = user.UserName;
            //safetyUser.AvatarUrl = user.AvatarUrl;
            //safetyUser.Gender = user.Gender;
            //safetyUser.PhoneNumber = user.PhoneNumber;
            //safetyUser.Email = user.Email;
            //safetyUser.UserStatus = user.UserStatus;
            //safetyUser.CreateTime = user.CreateTime;

            User safetyUser = await getSafetyUser(user);

            // Convert user object to JSON string
            var serializedSafetyUser = JsonConvert.SerializeObject(user);

            // add user into session
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString(USER_LOGIN_STATE)))
            {
                HttpContext.Session.SetString(USER_LOGIN_STATE, serializedSafetyUser);
            }

            safetyUser.IsAdmin = await verifyIsAdminRoleAsync();
            return safetyUser;
        }

        private async Task<User> getSafetyUser(User user)
        {
            User safetyUser = new User();
            safetyUser.Id = user.Id;
            safetyUser.UserName = user.UserName;
            safetyUser.AvatarUrl = user.AvatarUrl;
            safetyUser.Gender = user.Gender;
            safetyUser.PhoneNumber = user.PhoneNumber;
            safetyUser.Email = user.Email;
            safetyUser.UserStatus = user.UserStatus;
            safetyUser.CreateTime = user.CreateTime;

            return safetyUser;
        }

        private async Task<bool> verifyIsAdminRoleAsync()
        {
            var userState = HttpContext.Session.GetString(USER_LOGIN_STATE);
            if (string.IsNullOrWhiteSpace(userState))
            {
                return false;
            }


            // 1. verify permission role
            var loggedInUser = JsonConvert.DeserializeObject<User>(userState);
            var user = await userManager.FindByIdAsync(loggedInUser.Id.ToString());
            if (user == null || user.IsDelete)
            {
                return false;
            }

            var role_list = await userManager.GetRolesAsync(user);

            if (!role_list.Contains("admin"))
            {
                return false;
            }
            return true;
        }

        [HttpPost]
        public async Task<IEnumerable<User>?> searchUsers(string username)
        {

            // 1. verify permission role
            if (!await verifyIsAdminRoleAsync())
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var users = await userManager.Users.Where(u => u.UserName.Contains(username)&&u.IsDelete==false)
            .ToListAsync();

            // Create a list to store simplified user objects
            List<User> safetyUsers = new List<User>();

            // Loop through each user and call getSafetyUser to get simplified user object
            foreach (var user in users)
            {
                var safetyUser = await getSafetyUser(user);
                safetyUsers.Add(safetyUser);
            }

            // Return the list of simplified user objects
            return safetyUsers;
        }

        [HttpPost]
        public async Task<bool> deleteUser(long id)
        {
            // 1. verify permission role
            if (!await verifyIsAdminRoleAsync())
            {
                return false;
            }

            if (id < 0)
            {
                return false;
            }

            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return false;
            }
            if(user.IsDelete == true)
            {
                return false;
            }

            // user not null && user.IsDelete = False
            user.IsDelete = true;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return false; // Soft delete fail
            }

            return true; // Soft delete successful
        }
    }
}
