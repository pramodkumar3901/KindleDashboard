using KindleDashboard.Models;
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

var ipMonitor = app.Services.GetRequiredService<KindleIpMonitor>();

// Serve static files from wwwroot
app.UseStaticFiles();
app.UseDirectoryBrowser();

app.MapGet("/api/brightness/set", (int value) =>
{
  var script = Path.Combine(Directory.GetCurrentDirectory(), "kindlescripts", "kindle_Brightness.ps1");

  var psi = new System.Diagnostics.ProcessStartInfo()
  {
    FileName = "powershell",
    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" {ipMonitor.CurrentIp} set " + value,
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
  };

  var proc = System.Diagnostics.Process.Start(psi);
  string output = proc.StandardOutput.ReadToEnd();
  proc.WaitForExit();

  return Results.Json(new { ok = true, brightness = value });
});

app.MapGet("/api/getStatusDump", () =>
{
  var script = Path.Combine(Directory.GetCurrentDirectory(), "kindlescripts", "Kindle_FetchStatusDump.ps1");

  var psi = new System.Diagnostics.ProcessStartInfo()
  {
    FileName = "powershell",
    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" {ipMonitor.CurrentIp}",
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
  };

  var proc = System.Diagnostics.Process.Start(psi);
  string output = proc.StandardOutput.ReadToEnd();
  proc.WaitForExit();

  return Results.Json(new { ok = true, status = output });
});

app.MapGet("/api/gold", async () =>
{
  // Example – you can replace with your own logic or API call
  var goldPrice = await GetLatestGoldPriceAsync();

  return goldPrice;
});

async Task<IResult> GetLatestGoldPriceAsync()
{
  using var http = new HttpClient();

  var json = await http.GetStringAsync(
      "https://services.timesofindia.com/ufs-utility/utility/metals/get/price/trend/data?pc=toi&pfm=web&client=toi&metal=gold&city=kerala&quantityInGrams=8&purity=gold22&days=30&fv=1000"
  );

  using var doc = System.Text.Json.JsonDocument.Parse(json);

  var gold22 = doc.RootElement
      .GetProperty("metalPurityPrice")
      .GetProperty("gold22");

  // Dictionary: date -> price
  var dict = new Dictionary<DateOnly, double>();

  foreach (var kvp in gold22.EnumerateObject())
  {
    string dateText = kvp.Name;               // "23-11-2025"
    DateOnly date = DateOnly.ParseExact(dateText, "dd-MM-yyyy", null);

    double price = kvp.Value.GetDouble();

    dict[date] = price;
  }

  // Sort by date ascending
  var sorted = dict.OrderBy(x => x.Key).ToList();

  var xValues = sorted.Select(x => (DateOnly)x.Key).ToList(); 
  var yValues = sorted.Select(x => x.Value/1000).ToList();

  // Call PowerShell script and return Base64
  var _tableValue = gold22.GetRawText();
  return Results.Json(new { graph = RunPowerShellPlot(xValues, yValues), tableValue = _tableValue });

}

// Allow binding to LAN by using --urls argument or environment variables when running,
// e.g. dotnet run --urls http://0.0.0.0:5000

string RunPowerShellPlot(List<DateOnly> x, List<double> y)
{
  var xArg = string.Join(",", x);
  var yArg = string.Join(",", y);

  var script = Path.Combine(Directory.GetCurrentDirectory(), "kindlescripts", "Kindle_generateImage.ps1");

  var psi = new System.Diagnostics.ProcessStartInfo()
  {
    FileName = "powershell",
    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -XValues {xArg} -YValues {yArg}",
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
  };

  var proc = System.Diagnostics.Process.Start(psi);
  var output = proc!.StandardOutput.ReadToEnd();
  proc.WaitForExit(); 

  return output;
}


app.Run();