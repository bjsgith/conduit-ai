using ConduitAI.Data;
using ConduitAI.Services;
using ConduitAI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

LocalOnlyListenerPolicy.ValidateAllowedHosts(builder.Configuration["AllowedHosts"]);
LocalOnlyListenerPolicy.ValidateConfiguredListeners(builder.Configuration, builder.WebHost);

// MVC
builder.Services.AddControllersWithViews();

// EF Core / SQLite
var contentRoot = builder.Environment.ContentRootPath;
var connectionString = DatabasePaths.ResolveConnectionString(
    builder.Configuration.GetConnectionString("DefaultConnection"), contentRoot);
// Ensure the local data directory exists; SQLite will not create it.
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// Ollama configuration + typed HTTP client
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AiRequestLimiter>();
builder.Services.AddHttpClient<IOllamaClient, OllamaClient>()
    .ConfigurePrimaryHttpMessageHandler(OllamaHttpClientPolicy.CreatePrimaryHandler);

// AI helpers (stateless)
builder.Services.AddSingleton<AiPromptBuilder>();
builder.Services.AddSingleton<AiResponseParser>();

// Application services
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<IAiAnalysisService, AiAnalysisService>();
builder.Services.AddScoped<IMeetingNotesService, MeetingNotesService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFollowUpService, FollowUpService>();

var app = builder.Build();

// Apply migrations and seed demo data on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await LocalOnlyListenerPolicy.StartAndValidateAsync(app, builder.Configuration, builder.WebHost);
await app.WaitForShutdownAsync();

// Exposed for integration testing if needed.
public partial class Program { }
