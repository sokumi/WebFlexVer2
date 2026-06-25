using Microsoft.EntityFrameworkCore;
using WebFlex.UI.Services;
using WebFlex.UI.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WebFlexDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("WebFlexDb"),
        npgsqlOptions => {
            npgsqlOptions.MigrationsHistoryTable("s_migrationshistory", "public");
        })
    .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<TsdReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebFlexTsd")));

builder.Services.AddScoped<WebFlex.UI.Services.OpcBrowseService>();

builder.Services.AddSingleton<CurrentValueNotifyService>();
builder.Services.AddSingleton<WindowsServiceManager>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CurrentValueNotifyService>());

builder.Services.AddHttpClient();
builder.Services.AddScoped<TimescaleOptionService>();


var mvcBuilder = builder.Services.AddControllersWithViews();

if (builder.Environment.IsDevelopment()) {
    mvcBuilder.AddRazorRuntimeCompilation();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
