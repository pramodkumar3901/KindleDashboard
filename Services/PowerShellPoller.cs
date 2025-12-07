using KindleDashboard.Models;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;


namespace KindleDashboard.Services;


public class PowerShellPoller : BackgroundService
{
    private readonly ConcurrentDictionary<string, DeviceState> _state;
    private readonly ILogger<PowerShellPoller> _logger;
    private readonly string _scriptDir;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10); // polling interval


    public PowerShellPoller(ConcurrentDictionary<string, DeviceState> state, ILogger<PowerShellPoller> logger, IConfiguration config)
    {
        _state = state;
        _logger = logger;
        _scriptDir = Path.Combine(AppContext.BaseDirectory, "ps-scripts");
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PowerShellPoller started, scripts folder: {dir}", _scriptDir);


        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // run battery script
                var batteryJson = RunPowerShellScript("get_battery.ps1");
                if (!string.IsNullOrEmpty(batteryJson))
                {
                    try
                    {
                        var doc = JsonDocument.Parse(batteryJson);
                        var devId = doc.RootElement.GetProperty("DeviceId").GetString() ?? "kindle";


                        var ds = _state.GetOrAdd(devId, id => new DeviceState { DeviceId = id });


                        if (doc.RootElement.TryGetProperty("BatteryPercent", out var bp))
                            ds.BatteryPercent = bp.GetInt32();
                        if (doc.RootElement.TryGetProperty("BatteryRaw", out var br))
                            ds.BatteryRaw = br.GetString();


                        ds.LastUpdated = DateTimeOffset.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse battery JSON: {json}", batteryJson);
                    }
                }


                // run wifi script
                var wifiJson = RunPowerShellScript("get_wifi.ps1");
                if (!string.IsNullOrEmpty(wifiJson))
                {
                    try
                    {
                        var doc = JsonDocument.Parse(wifiJson);
                        var devId = doc.RootElement.GetProperty("DeviceId").GetString() ?? "kindle";


                        var ds = _state.GetOrAdd(devId, id => new DeviceState { DeviceId = id });


                        if (doc.RootElement.TryGetProperty("Wifi", out var w))
                            ds.Wifi = w.GetString();


                        ds.LastUpdated = DateTimeOffset.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse wifi JSON: {json}", wifiJson);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while polling scripts");
            }


            await Task.Delay(_interval, stoppingToken);
        }
    }
    private string? RunPowerShellScript(string scriptName)
    {
        var path = Path.Combine(_scriptDir, scriptName);
        if (!File.Exists(path))
        {
            _logger.LogWarning("Script not found: {path}", path);
            return null;
        }


        var psi = new ProcessStartInfo
        {
            FileName = "pwsh", // windows/linux: pwsh (PowerShell Core) - or use "powershell" on Windows if you prefer
            Arguments = $"-NoProfile -NonInteractive -File \"{path}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };


        using var p = Process.Start(psi);
        if (p == null) return null;


        string output = p.StandardOutput.ReadToEnd();
        string err = p.StandardError.ReadToEnd();
        p.WaitForExit(5000);


        if (!string.IsNullOrEmpty(err))
            _logger.LogDebug("Script {s} stderr: {err}", scriptName, err);


        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }
}