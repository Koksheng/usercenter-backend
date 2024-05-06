using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;
using usercenter_backend.Common;
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
                var result = await userManager.CreateAsync(user1, "12345678");
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
        public async Task<BaseResponse<long>> userRegister(UserRegisterRequest userRegisterRequest)
        {
            // userAccount = UserName in DB
            if (userRegisterRequest == null)
            {
                //return -1;
                return new BaseResponse<long>(-1, 0);
            }
            string userAccount = userRegisterRequest.userAccount;
            string userPassword = userRegisterRequest.userPassword;
            string checkPassword = userRegisterRequest.checkPassword;
            string planetCode = userRegisterRequest.planetCode;

            // 1. Verify
            if (string.IsNullOrWhiteSpace(userAccount) || string.IsNullOrWhiteSpace(userPassword) || string.IsNullOrWhiteSpace(checkPassword) || string.IsNullOrWhiteSpace(planetCode))
            {
                //return -1;
                return new BaseResponse<long>(-1, 0);
            }
            if (userAccount.Length < 4)
            {
                //return -1;
                return new BaseResponse<long>(-1, 0);
            }
            if (userPassword.Length < 8 || checkPassword.Length < 8)
                //return -1;
                return new BaseResponse<long>(-1, 0);

            if (planetCode.Length > 5)
                //return -1;
                return new BaseResponse<long>(-1, 0);

            // userAccount cant contain special character
            string pattern = @"[^a-zA-Z0-9\s]";
            if (Regex.IsMatch(userAccount, pattern))
            {
                //return -1;
                return new BaseResponse<long>(-1, 0);
            }
            // userPassword & checkPassword must same
            if (!userPassword.Equals(checkPassword))
            {
                //return -1;
                return new BaseResponse<long>(-1, 0);
            }

            // userAccount cant existed
            var user = await userManager.FindByNameAsync(userAccount);
            if (user != null)
            {
                if (user.IsDelete == false)
                    //return -1;
                    return new BaseResponse<long>(-1, 0);
            }

            // planetCode cant existed
            var planetCodeExists = await userManager.Users.AnyAsync(u => u.PlanetCode == planetCode && !u.IsDelete);
            if (planetCodeExists)
            {
                //return -1;
                return new BaseResponse<long>(-1, 0);
            }

            // 2. 加密 (.net core IdentityUser will encrypt themself

            // 3. Insert User to DB
            user = new User { UserName = userAccount, PlanetCode = planetCode };
            var result = await userManager.CreateAsync(user, userPassword);
            if (!result.Succeeded)
                //return -1;
                return new BaseResponse<long>(-1, 0);

            //return user.Id;
            //return new BaseResponse<long>(0, user.Id);
            return ResultUtils.success(user.Id);
        }

        private const string USER_LOGIN_STATE = "userLoginState";

        [HttpPost]
        public async Task<BaseResponse<User>?> userLogin(UserLoginResponse userLoginRequest)
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

            //safetyUser.IsAdmin = await verifyIsAdminRoleAsync();
            //return safetyUser;
            return ResultUtils.success(safetyUser);
        }

        [HttpPost]
        public async Task<BaseResponse<int>> userLogout()
        {
            var userState = HttpContext.Session.GetString(USER_LOGIN_STATE);
            if (string.IsNullOrWhiteSpace(userState))
            {
                //return -1;
                return new BaseResponse<int>(-1, 0);
            }
            HttpContext.Session.Remove(USER_LOGIN_STATE);
            //return 1;
            return ResultUtils.success(1);
        }

        private async Task<User> getSafetyUser(User user)
        {
            User safetyUser = new User();
            safetyUser.Id = user.Id;
            safetyUser.UserName = user.UserName;
            safetyUser.NormalizedUserName = user.NormalizedUserName;
            safetyUser.AvatarUrl = user.AvatarUrl;
            safetyUser.Gender = user.Gender;
            safetyUser.PhoneNumber = user.PhoneNumber;
            safetyUser.Email = user.Email;
            safetyUser.UserStatus = user.UserStatus;
            safetyUser.CreateTime = user.CreateTime;
            safetyUser.PlanetCode = user.PlanetCode;

            safetyUser.IsAdmin = await getIsAdmin(user);

            return safetyUser;
        }

        private async Task<bool> getIsAdmin(User user)
        {
            var role_list = await userManager.GetRolesAsync(user);

            if (!role_list.Contains("admin"))
            {
                return false;
            }
            return true;
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

            return await getIsAdmin(user);

            //var role_list = await userManager.GetRolesAsync(user);

            //if (!role_list.Contains("admin"))
            //{
            //    return false;
            //}
            //return true;
        }

        [HttpGet]
        public async Task<BaseResponse<List<User>>?> searchUsers(string? username)
        {

            // 1. verify permission role
            if (!await verifyIsAdminRoleAsync())
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                username = "";
            }

            var users = await userManager.Users.Where(u => u.UserName.Contains(username) && u.IsDelete == false)
            .ToListAsync();

            // Create a list to store simplified user objects
            List<User> safetyUsersList = new List<User>();

            // Loop through each user and call getSafetyUser to get simplified user object
            foreach (var user in users)
            {
                var safetyUser = await getSafetyUser(user);
                //safetyUser.IsAdmin = await getIsAdmin(user);
                safetyUsersList.Add(safetyUser);
            }

            // Return the list of simplified user objects
            //return safetyUsersList;
            return ResultUtils.success(safetyUsersList);
        }

        [HttpPost]
        public async Task<BaseResponse<bool>?> deleteUser(long id)
        {
            // 1. verify permission role
            if (!await verifyIsAdminRoleAsync())
            {
                //return false;
                return null;
            }

            if (id < 0)
            {
                //return false;
                return null;
            }

            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                //return false;
                return null;
            }
            if (user.IsDelete == true)
            {
                //return false;
                return null;
            }

            // user not null && user.IsDelete = False
            user.IsDelete = true;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                //return false; // Soft delete fail
                return null;
            }

            //return true; // Soft delete successful
            return ResultUtils.success(true);

        }

        [HttpGet]
        public async Task<BaseResponse<User>?> getCurrentUser()
        {
            var userState = HttpContext.Session.GetString(USER_LOGIN_STATE);
            if (string.IsNullOrWhiteSpace(userState))
            {
                return null;
            }


            // 1. get user by id
            var loggedInUser = JsonConvert.DeserializeObject<User>(userState);
            var user = await userManager.FindByIdAsync(loggedInUser.Id.ToString());
            if (user == null || user.IsDelete)
            {
                return null;
            }
            var safetyUser = await getSafetyUser(user);
            //safetyUser.IsAdmin = await verifyIsAdminRoleAsync();
            //return safetyUser;
            return ResultUtils.success(safetyUser);
        }
    }
}
