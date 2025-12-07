using System;


namespace KindleDashboard.Models;


public class DeviceState
{
public string DeviceId { get; set; } = string.Empty;
public int? BatteryPercent { get; set; }
public string? BatteryRaw { get; set; }
public string? Wifi { get; set; }
public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}