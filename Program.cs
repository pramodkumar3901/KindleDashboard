using KindleDashboard.Models;
using KindleDashboard.Endpoints;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using SkiaSharp;


var builder = WebApplication.CreateBuilder(args);

var ip = builder.Configuration["Server:Ip"] ?? "localhost";
var port = builder.Configuration["Server:Port"] ?? "5000";
builder.WebHost.UseUrls($"http://{ip}:{port}");

// Shared in-memory state (simple for start)
var deviceState = new ConcurrentDictionary<string, DeviceState>();


builder.Services.AddSingleton<KindleIpMonitor>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<KindleIpMonitor>());

// Make static files available (wwwroot)
builder.Services.AddDirectoryBrowser();

var app = builder.Build();


var monitor = app.Services.GetRequiredService<KindleIpMonitor>();
Console.WriteLine("Initial Kindle IP: " + monitor.CurrentIp);

// Serve static files from wwwroot
app.UseStaticFiles();
app.UseDirectoryBrowser();

// Map API endpoints
app.MapKindleEndpoints();

app.Run();