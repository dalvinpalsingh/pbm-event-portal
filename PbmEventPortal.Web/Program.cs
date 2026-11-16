using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Application.Services.Security;
using PbmEventPortal.Application.Services.TeamManagement;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.Security;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<PbmDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
builder.Services.AddScoped<ITeamManagementService, TeamManagementService>();
builder.Services.AddScoped<IUserContext, UserContext>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "PbmEventPortal.Auth";
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.Redirect(options.LoginPath);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(RoleEnums.SystemAdmin);
    });

    options.AddPolicy("TeamManagementWorkspaceAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(RoleEnums.SystemAdmin, RoleEnums.TeamManager);
    });
});

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "SystemAdminOnly");
    options.Conventions.AuthorizePage("/DbCheck", "SystemAdminOnly");
    options.Conventions.AuthorizeFolder("/Account");
    options.Conventions.AuthorizeFolder("/TeamManagement", "TeamManagementWorkspaceAccess");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change it for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
    var path = context.Request.Path;
    var mustChangePassword = string.Equals(
        context.User.FindFirst("MustChangePassword")?.Value,
        "true",
        StringComparison.OrdinalIgnoreCase);

    var isStaticAssetPath =
        path.StartsWithSegments("/css") ||
        path.StartsWithSegments("/js") ||
        path.StartsWithSegments("/lib") ||
        path.StartsWithSegments("/favicon.ico");

    var isProtectedPath =
        path.StartsWithSegments("/Admin") ||
        path.StartsWithSegments("/Account") ||
        path.StartsWithSegments("/DbCheck") ||
        path.StartsWithSegments("/TeamManagement");

    if (!isStaticAssetPath && (isAuthenticated || isProtectedPath))
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }

    if (isAuthenticated && mustChangePassword)
    {
        var isAllowedPath =
            path.StartsWithSegments("/Account/ChangePassword") ||
            path.StartsWithSegments("/Logout") ||
            path.StartsWithSegments("/Login") ||
            isStaticAssetPath;

        if (!isAllowedPath)
        {
            context.Response.Redirect("/Account/ChangePassword");
            return;
        }
    }

    await next();
});

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
