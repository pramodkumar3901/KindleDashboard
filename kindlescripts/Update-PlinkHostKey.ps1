param(
    [Parameter(Mandatory=$true)]
    [string]$IpAddress,
            
    [string]$HostKey = "0x49a67e8d01f64e0d4e99808922b96d5e3f4b92b7039aacd0f4b8d11c93f001d,0x793e78280fbb9261850a41fc3f0bbe22011b71e7169b42d84d64f73baf125372"
)

    $baseKey = "HKCU:\Software\SimonTatham\PuTTY\SshHostKeys"
    $valueName = "ssh-ed25519@22:$IpAddress"

    # Create registry key if it doesn't exist
    if (!(Test-Path $baseKey)) {
        New-Item -Path $baseKey -Force | Out-Null
    }

    # Check if exact IP+key combo already exists
    $existing = Get-ItemProperty -Path $baseKey -Name $valueName -ErrorAction SilentlyContinue
    if ($existing.$valueName -eq $HostKey) {
        Write-Host "[Plink] No update needed (same IP + key already exists)."
        return
    }

    # Remove any entry that has the same host key (old IPs)
    $properties = Get-ItemProperty -Path $baseKey
    foreach ($prop in $properties.PSObject.Properties) {
        if ($prop.Name -like "ssh-ed25519@22:*" -and $prop.Value -eq $HostKey) {
            Remove-ItemProperty -Path $baseKey -Name $prop.Name
            Write-Host "[Plink] Removed old host entry: $($prop.Name)"
        }
    }

    # Remove any outdated entry for same IP (wrong key)
    if (Get-ItemProperty -Path $baseKey -Name $valueName -ErrorAction SilentlyContinue) {
        Remove-ItemProperty -Path $baseKey -Name $valueName
        Write-Host "[Plink] Removed old key for IP: $IpAddress"
    }

# Add the new IP → hostKey mapping
Set-ItemProperty -Path $baseKey -Name $valueName -Value $HostKey
Write-Host "[Plink] Host key updated → $valueName"