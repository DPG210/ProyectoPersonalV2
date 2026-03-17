using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProyectoPersonal.Data;
using ProyectoPersonal.Hubs;
using ProyectoPersonal.Policies;
using ProyectoPersonal.Repositories;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews
    (options => options.EnableEndpointRouting = false);
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddTransient<RepositoryTrivial>();
builder.Services.AddTransient<MailKitService>();
string connectionString = builder.Configuration.GetConnectionString("SqlTrivial");
builder.Services.AddDbContext<TrivialContext>
    (options => options.UseSqlServer(connectionString));
builder.Services.AddAuthentication
    (options =>
    {
        options.DefaultAuthenticateScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;
    }).AddCookie(
    CookieAuthenticationDefaults.AuthenticationScheme,
    config =>
    {
        config.AccessDeniedPath = "/Managed/ErrorAcceso";
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SoloPremium",
        policy => policy.Requirements.Add(new GetRolesRequirement()));
    options.AddPolicy("SoloAdmin",
        policy => policy.Requirements.Add(new SerAdminRequirement()));
});
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Striped")["SecretKey"];

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.UseMvc(routes =>
{
    routes.MapRoute(name: "default", template: "{controller=Managed}/{action=Login}/{id?}");
});

app.MapHub<TrivialHub>("/trivialHub");

app.Run();
