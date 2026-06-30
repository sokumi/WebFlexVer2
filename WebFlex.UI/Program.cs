using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using WebFlex.UI.Data;
using WebFlex.UI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WebFlexDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("WebFlexDb"),
        npgsqlOptions => {
            npgsqlOptions.MigrationsHistoryTable("s_migrationshistory", "public");
        })
    .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<INewNoService, NewNoService>();

builder.Services.AddDbContext<TsdReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebFlexTsd")));

builder.Services.AddScoped<WebFlex.UI.Services.OpcBrowseService>();

builder.Services.AddSingleton<CurrentValueNotifyService>();
builder.Services.AddSingleton<WindowsServiceManager>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CurrentValueNotifyService>());

builder.Services.AddHttpClient();
builder.Services.AddScoped<TimescaleOptionService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.Cookie.Name = "WebFlex.Auth";
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var mvcBuilder = builder.Services.AddControllersWithViews(options => {
    options.Filters.Add(new AuthorizeFilter());
});

if (builder.Environment.IsDevelopment()) {
    mvcBuilder.AddRazorRuntimeCompilation();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();