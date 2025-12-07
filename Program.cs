using KindleDashboard.Models;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using SkiaSharp;


var builder = WebApplication.CreateBuilder(args);


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

app.MapGet("/api/brightness/get", () =>
{
  var script = Path.Combine(Directory.GetCurrentDirectory(), "kindlescripts", "kindle_Brightness.ps1");

  var psi = new System.Diagnostics.ProcessStartInfo()
  {
    FileName = "powershell",
    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" {ipMonitor.CurrentIp} get",
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
  };

  var proc = System.Diagnostics.Process.Start(psi);
  string output = proc.StandardOutput.ReadToEnd();
  proc.WaitForExit();

  int level = 0;
  int.TryParse(output, out level);
  
  return Results.Json(new { brightness = level });
});

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

app.MapGet("/api/wifi/get", () =>
{
  var script = Path.Combine(builder.Environment.ContentRootPath,
      "kindlescripts", "Kindle_wifi.ps1");

  if (!File.Exists(script))
    return Results.Problem($"Script not found: {script}");

  var psi = new ProcessStartInfo
  {
    FileName = "powershell",
    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" {ipMonitor.CurrentIp}",
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
  };

  var proc = Process.Start(psi);
  string output = proc.StandardOutput.ReadToEnd();
  proc.WaitForExit();

  string mac = "";
  string ap = "";
  string ip = "";

  // Lines
  var lines = output.Split('\n', StringSplitOptions.TrimEntries);

  foreach (var line in lines)
  {
    // 2.1 MAC
    if (line.StartsWith("2.1 MAC:", StringComparison.OrdinalIgnoreCase))
    {
      mac = line.Replace("2.1 MAC:", "").Trim();
    }

    // 2.3 AP
    if (line.StartsWith("2.3 AP:", StringComparison.OrdinalIgnoreCase))
    {
      // Extract text before first "("
      var temp = line.Replace("2.3 AP:", "").Trim();
      int idx = temp.IndexOf(" (");
      ap = idx > 0 ? temp.Substring(0, idx) : temp;
    }

    // 4.1 IP
    if (line.StartsWith("4.1  IP", StringComparison.OrdinalIgnoreCase))
    {
      var parts = line.Split(':');
      if (parts.Length > 1)
        ip = parts[1].Trim();
    }
  }

  return Results.Json(new
  {
    mac = mac,
    ap = ap,
    ip = ip
  });
});

app.MapGet("/api/battery", () =>
{
  var script = Path.Combine(Directory.GetCurrentDirectory(), "kindlescripts", "Kindle_Battery.ps1");

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

  return Results.Json(new { ok = true, battery = output });
});

app.MapGet("/api/gold/graph", async () =>
{
  string url = "https://services.timesofindia.com/ufs-utility/utility/metals/get/price/trend/data?pc=toi&pfm=web&client=toi&metal=gold&city=kerala&quantityInGrams=8&purity=gold22&days=7&fv=1000";

  using var http = new HttpClient();
  var json = await http.GetStringAsync(url);

  var obj = System.Text.Json.JsonDocument.Parse(json).RootElement;

  var goldData = obj
      .GetProperty("metalPurityPrice")
      .GetProperty("gold22");

  // Parse into lists
  var dates = new List<DateTime>();
  var prices = new List<int>();

  foreach (var p in goldData.EnumerateObject())
  {
    var dt = DateTime.ParseExact(p.Name, "dd-MM-yyyy", CultureInfo.InvariantCulture);
    dates.Add(dt);
    prices.Add(p.Value.GetInt32());
  }

  // Sort by date
  var combined = dates.Zip(prices, (d, v) => new { d, v }).OrderBy(x => x.d).ToList();
  dates = combined.Select(x => x.d).ToList();
  prices = combined.Select(x => x.v).ToList();

  // Draw graph
  int width = 600, height = 300;
  using var surface = SKSurface.Create(new SKImageInfo(width, height));
  var canvas = surface.Canvas;
  canvas.Clear(SKColors.White);

  using var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2 };
  using var linePaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 3, IsAntialias = true };
  using var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 18 };

  // Axes
  canvas.DrawLine(40, 20, 40, height - 40, axisPaint);
  canvas.DrawLine(40, height - 40, width - 20, height - 40, axisPaint);

  // Scale prices
  int minP = prices.Min() - 200;
  int maxP = prices.Max() + 200;
  float scale = (height - 60f) / (maxP - minP);

  // Plot lines
  for (int i = 1; i < prices.Count; i++)
  {
    float x1 = 40 + (i - 1) * (500f / (prices.Count - 1));
    float y1 = height - 40 - (prices[i - 1] - minP) * scale;

    float x2 = 40 + i * (500f / (prices.Count - 1));
    float y2 = height - 40 - (prices[i] - minP) * scale;

    canvas.DrawLine(x1, y1, x2, y2, linePaint);
  }

  // Title
  canvas.DrawText("Kerala Gold Price (22K / 8g)", 150, 20, textPaint);

  // Encode PNG
  using var img = surface.Snapshot();
  using var data = img.Encode(SKEncodedImageFormat.Png, 100);

  return Results.File(data.ToArray(), "image/png");
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
      "https://services.timesofindia.com/ufs-utility/utility/metals/get/price/trend/data?pc=toi&pfm=web&client=toi&metal=gold&city=kerala&quantityInGrams=8&purity=gold22&days=7&fv=1000"
  );

  using var doc = System.Text.Json.JsonDocument.Parse(json);

  var gold22 = doc.RootElement
      .GetProperty("metalPurityPrice")
      .GetProperty("gold22");

  // Dictionary: date -> price
  var dict = new Dictionary<int, double>();

  foreach (var kvp in gold22.EnumerateObject())
  {
    string dateText = kvp.Name;               // "23-11-2025"
    int day = int.Parse(dateText[..2]);       // take only "23"

    double price = kvp.Value.GetDouble();

    dict[day] = price;
  }

  // Sort by date ascending
  var sorted = dict.OrderBy(x => x.Key).ToList();

  var xValues = sorted.Select(x => (double)x.Key).ToList();   // [18,19,20,21,22,23]
  var yValues = sorted.Select(x => x.Value/1000).ToList();

  // Call PowerShell script and return Base64
  var _tableValue = gold22.GetRawText();
  return Results.Json(new { graph = RunPowerShellPlot(xValues, yValues), tableValue = _tableValue });

}

// Allow binding to LAN by using --urls argument or environment variables when running,
// e.g. dotnet run --urls http://0.0.0.0:5000

string RunPowerShellPlot(List<double> x, List<double> y)
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