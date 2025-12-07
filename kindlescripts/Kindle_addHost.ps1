param(
    [Parameter(Mandatory=$true)]
    [string]$IpAddress,

    [string]$HostKey = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIHJTEq8792RN2EKbFudxGwEivgs//EEKhWGSuw8oeD75"
)

# Path to known_hosts file
$knownHosts = "$env:USERPROFILE\.ssh\known_hosts"

# Ensure directory exists
$dir = Split-Path $knownHosts
if (!(Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

# Ensure file exists
if (!(Test-Path $knownHosts)) {
    New-Item -ItemType File -Path $knownHosts -Force | Out-Null
}

# Read current lines
$lines = Get-Content $knownHosts -ErrorAction SilentlyContinue

# Remove any line that contains the *same host key*
$keyPattern = [regex]::Escape($HostKey)
$lines = $lines | Where-Object { $_ -notmatch "\s+$keyPattern$" }

# Remove any line with the same IP (if accidentally present)
$ipPattern = [regex]::Escape($IpAddress)
$lines = $lines | Where-Object { $_ -notmatch "^$ipPattern\s" }

# Add new entry
$newLine = "$IpAddress $HostKey"
$lines += $newLine

# Write final output
$lines | Set-Content $knownHosts -Encoding ascii

Write-Host "Updated known_hosts: Added '$newLine' and removed duplicates."
