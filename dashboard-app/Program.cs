using System.Runtime.CompilerServices;
using DashboardApi.Data;
using DashboardApi.Models;
using DashboardApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// Registers AppDbContext with .NET's dependency injection system
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();
builder.Services.AddHostedService<HealthCheckService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/services", async (AppDbContext db) =>
{
    var cutoff = DateTime.UtcNow.AddHours(-24);

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var raw = await db.Services
        .Select(s => new
        {
            Service = s,
            LatestCheck = s.StatusChecks
                .OrderByDescending(c => c.CheckedAt)
                .FirstOrDefault(),
            RecentTotal = s.StatusChecks.Count(c => c.CheckedAt >= cutoff),
            RecentUp = s.StatusChecks.Count(c => c.CheckedAt >= cutoff && c.IsUp)
        })
        .ToListAsync();

    Console.WriteLine($"[TIMING] Query took {sw.ElapsedMilliseconds}ms");

    var services = raw.Select(x => new ServiceDto
    {
        Id = x.Service.Id,
        Name = x.Service.Name,
        Url = x.Service.Url,
        Description = x.Service.Description,
        LastKnownStatus = x.LatestCheck != null ? (bool?)x.LatestCheck.IsUp : null,
        LastCheckedAt = x.LatestCheck != null ? (DateTime?)x.LatestCheck.CheckedAt : null,
        LastResponseTimeMs = x.LatestCheck != null ? (int?)x.LatestCheck.ResponseTimeMs : null,
        UptimePercent24h = x.RecentTotal > 0
            ? Math.Round((double)x.RecentUp / x.RecentTotal * 100.0, 1)
            : (double?)null
    }).ToList();

    return Results.Ok(services);
});

app.MapGet("/services/{id}/checks", async (int id, AppDbContext db) =>
{
    var checks = await db.StatusChecks
        .Where(c => c.ServiceId == id)
        .OrderByDescending(c => c.CheckedAt)
        .ToListAsync();
    return Results.Ok(checks);
});

app.MapPost("/services", async (AppDbContext db, Service newService) =>
{
    db.Services.Add(newService);
    await db.SaveChangesAsync();
    return Results.Created($"/services/{newService.Id}", newService);
});

app.MapGet("/services/{id}", async (int id, AppDbContext db) =>
{
    var service = await db.Services.FindAsync(id);
    return service is not null ? Results.Ok(service) : Results.NotFound();
});

app.MapDelete("/services/{id}", async (int id, AppDbContext db) =>
{
    var service = await db.Services.FindAsync(id);
    if (service is null) return Results.NotFound();

    db.Services.Remove(service);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.UseHttpsRedirection();
app.Run();
