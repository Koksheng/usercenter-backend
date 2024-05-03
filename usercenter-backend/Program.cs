using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using usercenter_backend;
using usercenter_backend.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Start of Serilog
var logger = new LoggerConfiguration()
  .ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
// End of Serilog

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
});


builder.Services.AddDbContext<MyDbContext>(opt =>
{
    //opt.UseSqlServer("Server=.;Database=usercenter;Trusted_Connection=True;TrustServerCertificate=True;");
    opt.UseSqlServer("Server=(localdb)\\Local;Database=usercenter;Trusted_Connection=True;TrustServerCertificate=True;");
});
builder.Services.AddDataProtection();
builder.Services.AddIdentityCore<User>(options =>
{
    //options.Lockout.MaxFailedAccessAttempts = 5;
    //options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(5);
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
});
IdentityBuilder idBuilder = new IdentityBuilder(typeof(User), typeof(Role), builder.Services);
idBuilder.AddEntityFrameworkStores<MyDbContext>().AddDefaultTokenProviders().AddUserManager<UserManager<User>>().AddRoleManager<RoleManager<Role>>();


var app = builder.Build();

app.UseSession();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
