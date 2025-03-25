using Khronos4.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http;
using NLog;
using NLog.Web;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc; // Added for logging

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection String: {connectionString}");

builder.Services.AddRazorPages();
builder.Services.AddScoped<BasePageModel>();

// Register HttpClient
builder.Services.AddHttpClient();

// Authentication & Sessions
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

/*Temp*/
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = int.MaxValue;    // number of values
    options.KeyLengthLimit = int.MaxValue;     // length of keys
    options.ValueLengthLimit = int.MaxValue;   // length of values
    options.MultipartBodyLengthLimit = long.MaxValue; // total request size
});
/*Temp*/
builder.Services.AddRazorPages().AddRazorPagesOptions(options =>
{
    options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
});


// Configure Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Logging Services
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole(); // Add console logging
    loggingBuilder.AddDebug();   // Add debug logging
    // Add other logging providers as needed (e.g., file, database)
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseSession();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

// Redirect root to login
app.MapGet("/", context =>
{
    context.Response.Redirect("/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();
app.MapControllers();

app.Run();