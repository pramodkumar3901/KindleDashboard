using System.Diagnostics;

public class KindleIpMonitor : BackgroundService
{
  private readonly string _scriptPath;
  private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

  public string CurrentIp { get; private set; } = "";
  public event Action<string> OnIpChanged;

  public KindleIpMonitor(IHostEnvironment env)
  {
    _scriptPath = Path.Combine(env.ContentRootPath, "kindlescripts", "Kindle_ipAddress.ps1");
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        string newIp = FetchIp();
        if (!string.IsNullOrWhiteSpace(newIp) && newIp != CurrentIp)
        {
          CurrentIp = newIp;

          // IMPORTANT: provide your Kindle's fixed host key here
          //string hostKey = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIHJTEq8792RN2EKbFudxGwEivgs//EEKhWGSuw8oeD75";
          string hostKey = "0x49a67e8d01f64e0d4e99808922b96d5e3f4b92b7039aacd0f4b8d11c93f001d,0x793e78280fbb9261850a41fc3f0bbe22011b71e7169b42d84d64f73baf125372";

          UpdatePlinkHostKey(newIp, hostKey);

          OnIpChanged?.Invoke(newIp);
          Console.WriteLine($"[Kindle IP Updated] → {newIp}");
        }
      }
      catch { /* swallow */ }

      await Task.Delay(_interval, stoppingToken);
    }
  }

  private string FetchIp()
  {
    if (!File.Exists(_scriptPath))
      return "";

    var psi = new ProcessStartInfo()
    {
      FileName = "powershell",
      Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{_scriptPath}\"",
      RedirectStandardOutput = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    var proc = Process.Start(psi);
    string ip = proc.StandardOutput.ReadToEnd().Trim();
    proc.WaitForExit();

    return ip;
  }

  private void UpdateSSHHostKey(string ipAddress, string hostKey)
  {
    string knownHosts = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".ssh",
        "known_hosts"
    );

    // Ensure directory exists
    Directory.CreateDirectory(Path.GetDirectoryName(knownHosts)!);

    // Ensure file exists
    if (!File.Exists(knownHosts))
      File.WriteAllText(knownHosts, "");

    var lines = File.ReadAllLines(knownHosts).ToList();

    string newEntry = $"{ipAddress} {hostKey}";

    // 1) If exact same IP + key is already present → DO NOTHING
    if (lines.Any(l => l.Trim() == newEntry))
    {
      Console.WriteLine("[known_hosts] No update needed (same IP + key already exists).");
      return;
    }

    // 2) Remove any line containing the same host key (different IPs)
    lines = lines.Where(line => !line.EndsWith(hostKey)).ToList();

    // 3) Remove any line that starts with this IP (old incorrect mapping)
    lines = lines.Where(line => !line.StartsWith(ipAddress + " ")).ToList();

    // 4) Add the new correct entry
    lines.Add(newEntry);

    File.WriteAllLines(knownHosts, lines);

    Console.WriteLine($"[known_hosts updated] → {newEntry}");
  }

  private void UpdatePlinkHostKey(string ipAddress, string hostKey)
  {
    const string baseKey = @"Software\SimonTatham\PuTTY\SshHostKeys";

    // Determine the registry value name format used by Plink
    // Plink/Putty uses: "ed25519@22:IP"
    string valueName = $"ssh-ed25519@22:{ipAddress}";

    using var puTTYKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(baseKey);

    if (puTTYKey == null)
    {
      Console.WriteLine("[Plink] Unable to access PuTTY registry key.");
      return;
    }

    // 1) Check if exact IP+key combo already exists → DO NOTHING
    object? existing = puTTYKey.GetValue(valueName);
    if (existing != null && existing.ToString() == hostKey)
    {
      Console.WriteLine("[Plink] No update needed (same IP + key already exists).");
      return;
    }

    // 2) Remove any entry that has the same host key (old IPs)
    foreach (var name in puTTYKey.GetValueNames())
    {
      var val = puTTYKey.GetValue(name)?.ToString();
      if (val == hostKey)
      {
        puTTYKey.DeleteValue(name);
        Console.WriteLine($"[Plink] Removed old host entry: {name}");
      }
    }

    // 3) Remove any outdated entry for same IP (wrong key)
    if (puTTYKey.GetValue(valueName) != null)
    {
      puTTYKey.DeleteValue(valueName);
      Console.WriteLine($"[Plink] Removed old key for IP: {ipAddress}");
    }

    // 4) Add the new IP → hostKey mapping
    puTTYKey.SetValue(valueName, hostKey);
    Console.WriteLine($"[Plink] Host key updated → {valueName}");
  }


}
