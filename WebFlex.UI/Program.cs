using Microsoft.EntityFrameworkCore;
using WebFlex.UI.Services.CurrentValue;
using WebFlex.UI.Data;
using WebFlex.UI.Services.Timescale;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WebFlexDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("WebFlexDb"),
        npgsqlOptions => {
            npgsqlOptions.MigrationsHistoryTable("s_migrationshistory", "public");
        }
    ));

builder.Services.AddDbContext<TsdReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebFlexTsd")));

builder.Services.AddScoped<WebFlex.UI.Services.Device.OpcBrowseService>();

builder.Services.AddSingleton<CurrentValueNotifyService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CurrentValueNotifyService>());

builder.Services.AddHttpClient();
builder.Services.AddScoped<TimescaleOptionService>();

builder.Services.AddControllersWithViews();

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
