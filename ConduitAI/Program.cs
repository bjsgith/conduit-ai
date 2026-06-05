using ConduitAI.Data;
using ConduitAI.Services;
using ConduitAI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// EF Core / SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=App_Data/conduitai.db";
// Ensure the local data directory exists; SQLite will not create it.
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// Ollama configuration + typed HTTP client
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddHttpClient<IOllamaClient, OllamaClient>();

// AI helpers (stateless)
builder.Services.AddSingleton<AiPromptBuilder>();
builder.Services.AddSingleton<AiResponseParser>();

// Application services
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<IAiAnalysisService, AiAnalysisService>();
builder.Services.AddScoped<IMeetingNotesService, MeetingNotesService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

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

app.Run();

// Exposed for integration testing if needed.
public partial class Program { }
