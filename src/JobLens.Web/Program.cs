using JobLens.Application.Analysis;
using JobLens.Application.Prompts;
using JobLens.Domain.Abstractions;
using JobLens.Infrastructure.Llm;
using JobLens.Infrastructure.Persistence;
using JobLens.Infrastructure.Profiles;
using JobLens.Infrastructure.Scraping;
using JobLens.Web.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

builder.Services.AddHttpClient<ILlmProvider, GeminiProvider>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
});

builder.Services.AddHttpClient<IJobPostingFetcher, HtmlJobFetcher>();

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "joblens.db");
builder.Services.AddDbContext<JobLensDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();

var profilePath = Path.Combine(builder.Environment.ContentRootPath, "profile.default.json");
builder.Services.AddSingleton<IUserProfileStore>(_ => new JsonFileUserProfileStore(profilePath));

builder.Services.AddSingleton<PromptBuilder>();
builder.Services.AddScoped<AnalysisService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<JobLensDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
