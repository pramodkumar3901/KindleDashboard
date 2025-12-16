using System.Diagnostics;
using System.Text.Json;
using KindleDashboard.Models;

namespace KindleDashboard.Endpoints;

public static class ApiEndpoints
{
    public static void MapKindleEndpoints(this WebApplication app)
    {
        var monitor = app.Services.GetRequiredService<KindleIpMonitor>();

        app.MapGet("/api/brightness/set", (int value, KindleIpMonitor ipMonitor) =>
        {
            var script = Path.Combine(Directory.GetCurrentDirectory(), "kindlescripts", "kindle_Brightness.ps1");

            var psi = new ProcessStartInfo()
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" {ipMonitor.CurrentIp} set " + value,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            return Results.Json(new { ok = true, brightness = value });
        });

        app.MapGet("/api/getStatusDump", (KindleIpMonitor ipMonitor) =>
        {
            var script = Path.Combine(Directory.GetCurrentDirectory(), "kindlescripts", "Kindle_FetchStatusDump.ps1");

            var psi = new ProcessStartInfo()
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

            var arpPsi = new ProcessStartInfo
            {
                FileName = "arp",
                Arguments = "-a",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var arpProc = Process.Start(arpPsi);
            string arpOutput = arpProc.StandardOutput.ReadToEnd();
            arpProc.WaitForExit();
            bool _printerOnline = arpOutput.Contains("5c-60-ba-83-f5-32", StringComparison.OrdinalIgnoreCase);

            return Results.Json(new { ok = true, status = output, printerOnline = _printerOnline });
        });

        app.MapGet("/api/gold", async () =>
        {
            var goldPrice = await GetLatestGoldPriceAsync();
            return goldPrice;
        });
    }

    private static async Task<IResult> GetLatestGoldPriceAsync()
    {
        using var http = new HttpClient();

        var json = await http.GetStringAsync(
            "https://services.timesofindia.com/ufs-utility/utility/metals/get/price/trend/data?pc=toi&pfm=web&client=toi&metal=gold&city=kerala&quantityInGrams=8&purity=gold22&days=30&fv=1000"
        );

        using var doc = JsonDocument.Parse(json);

        var gold22 = doc.RootElement
            .GetProperty("metalPurityPrice")
            .GetProperty("gold22");

        var dict = new Dictionary<DateOnly, double>();

        foreach (var kvp in gold22.EnumerateObject())
        {
            try 
            {
                  string dateText = kvp.Name; 
                  DateOnly date = DateOnly.ParseExact(dateText, "dd-MM-yyyy", null);
                  double price = kvp.Value.GetDouble();
                  dict[date] = price;
            }
            catch {}
        }

        var sorted = dict.OrderBy(x => x.Key).ToList();

        var xValues = sorted.Select(x => (DateOnly)x.Key).ToList();
        var yValues = sorted.Select(x => x.Value / 1000).ToList();

        var _tableValue = gold22.GetRawText();
        return Results.Json(new { graph = RunPowerShellPlot(xValues, yValues), tableValue = _tableValue });
    }

    private static string RunPowerShellPlot(List<DateOnly> x, List<double> y)
    {
        var xArg = string.Join(",", x);
        var yArg = string.Join(",", y);

        var script = Path.Combine(Directory.GetCurrentDirectory(), "kindlescripts", "Kindle_generateImage.ps1");

        var psi = new ProcessStartInfo()
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -XValues {xArg} -YValues {yArg}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var proc = Process.Start(psi);
        var output = proc!.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        return output;
    }
}
