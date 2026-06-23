using LandIt.Data;
using LandIt.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LandIt.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<AppUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});


// to be used by the ATS score and the resume builder
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddHttpClient<GroqService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<GroqService>();
builder.Services.AddScoped<ResumeExtractorService>();
builder.Services.AddScoped<ResumePdfBuilder>();


// To be used for redirecting users to their respective dashboards after login
builder.Services.AddScoped<NavigationService>();

// Stripe
builder.Services.AddScoped<StripeService>();
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    try
    {
        await SeedDataAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during seeding.");
        throw;
    }
}

// Short-URL aliases for the Admin dashboard root + profile
app.MapControllerRoute(
    name: "admin_index",
    pattern: "Admin/Index",
    defaults: new { area = "Admin", controller = "Admin", action = "Index" });

app.MapControllerRoute(
    name: "admin_profile",
    pattern: "Admin/Profile",
    defaults: new { area = "Admin", controller = "Admin", action = "Profile" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();


async Task SeedDataAsync(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    //  ROLES 
    string[] roles = { "Admin", "Recruiter", "Candidate" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    //  ADMIN 
    string adminEmail = "farahhh.adel@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new AppUser
        {
            FullName = "Farah Masri",
            Photo = "/images/users/test.jpg",
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            Region = Region.MiddleEast,
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");

        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // RECRUITER USER 
    string recruiterEmail = "masrinoor84@gmail.com";
    var recruiterUser = await userManager.FindByEmailAsync(recruiterEmail);

    if (recruiterUser == null)
    {
        recruiterUser = new AppUser
        {
            FullName = "Noor Masri",
            UserName = recruiterEmail,
            Email = recruiterEmail,
            EmailConfirmed = true,
            IsRecruiter = true,
            Region = Region.NorthAmerica,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(recruiterUser, "Recruiter123!");

        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(recruiterUser, "Recruiter");
    }

    // RECRUITER PROFILE
    var existingRecruiter = dbContext.Recruiters
        .FirstOrDefault(r => r.UserId == recruiterUser.Id);

    if (existingRecruiter == null)
    {
        var recruiter = new Recruiter
        {
            UserId = recruiterUser.Id,
            FullName = "Noor Masri",
            Company = "Amazon",
            Title = "Senior Talent Acquisition Specialist",
            Region = Region.NorthAmerica,
            HourlyRate = 56.88M,
            Skills = "Communication Skills, HR and labor laws, Data Analysis, Business Analysis, Market Research, Negotiation, Compensation"
        };

        dbContext.Recruiters.Add(recruiter);
        await dbContext.SaveChangesAsync();
    }
}