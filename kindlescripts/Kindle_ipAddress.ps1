param(
    [string]$Mac
)

# Default MAC address (change this to your Kindle's MAC)
$DefaultMac = "d4-91-0f-70-0c-d3"

# Use provided MAC, or fallback to default
if ([string]::IsNullOrWhiteSpace($Mac)) {
    $Mac = $DefaultMac
}

# Normalize input MAC (remove separators, convert to lowercase)
$normalizedMac = $Mac.ToLower().Replace("-", "").Replace(":", "")

# Run ARP
$arp = arp -a

foreach ($line in $arp) {

    if ($line.Trim() -eq "") { continue }

    # Split ARP row into parts
    $parts = $line -split "\s+"
    if ($parts.Count -lt 3) { continue }

    $ip = $parts[1]
    $macAddr = $parts[2].ToLower()

    # Normalize MAC format
    $macClean = $macAddr.Replace("-", "").Replace(":", "")

    if ($macClean -eq $normalizedMac) {
        Write-Output $ip
        exit 0
    }
}

# Not found
Write-Output ""
exit 1
