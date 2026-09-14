using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

// Add authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Mock Authentication (Cookie-based for training purposes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("TeamLead", policy => policy.RequireRole("TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
if (builder.Configuration.GetValue("AzureStorage:ScanQueueEnabled", false))
{
    builder.Services.AddSingleton<IDocumentScanQueue, AzureDocumentScanQueue>();
}
else
{
    builder.Services.AddSingleton<IDocumentScanQueue, LocalDocumentScanQueue>();
}

// Add HttpContextAccessor for accessing user claims
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated(); // For development - use migrations in production
        EnsureDocumentScanColumns(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

static void EnsureDocumentScanColumns(ApplicationDbContext context)
{
    var connection = context.Database.GetDbConnection();
    var wasClosed = connection.State == System.Data.ConnectionState.Closed;

    if (wasClosed)
    {
        connection.Open();
    }

    try
    {
        foreach (var column in new[]
        {
            (Name: "Tags", Definition: "TEXT NULL"),
            (Name: "ScanStatus", Definition: "TEXT NOT NULL DEFAULT 'Clean'"),
            (Name: "ScanContentHash", Definition: "TEXT NULL"),
            (Name: "ScanUpdatedAtUtc", Definition: "TEXT NULL"),
            (Name: "ScanError", Definition: "TEXT NULL")
        })
        {
            using var existsCommand = connection.CreateCommand();
            existsCommand.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Documents') WHERE name = $name";
            var parameter = existsCommand.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = column.Name;
            existsCommand.Parameters.Add(parameter);

            if (Convert.ToInt32(existsCommand.ExecuteScalar()) == 0)
            {
                using var addColumnCommand = connection.CreateCommand();
                addColumnCommand.CommandText = $"ALTER TABLE Documents ADD COLUMN {column.Name} {column.Definition}";
                addColumnCommand.ExecuteNonQuery();
            }
        }
    }
    finally
    {
        if (wasClosed)
        {
            connection.Close();
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Use HSTS even in development for training purposes
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy for Blazor Server
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:;";
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapControllers();
app.MapFallbackToPage("/_Host");

app.Run();
