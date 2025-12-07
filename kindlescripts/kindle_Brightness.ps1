param(

	[Parameter(Mandatory = $true)]
    [string]$KindleIP,
	
    [Parameter(Mandatory = $true)]
    [string]$Action,    # "set" or "get"

    [int]$Level         # only needed for "set"
)

$User      = "root"
$Password  = "K1NdI3"
$Plink     = "plink"

$BrightFile = "/sys/class/backlight/bl/brightness"

function Set-Brightness {
    param([int]$Level)

    # Validate 0-10
    if ($Level -lt 0 -or $Level -gt 10) {
        Write-Host "Error: brightness level must be between 0 and 10."
        exit
    }

    # Convert 0-10 scale into 0-2399
    $KindleValue = [math]::Round($Level * 239.9)

    $Cmd = "sh -c 'echo $KindleValue > $BrightFile'"

    & $Plink -ssh "$User@$KindleIP" -pw $Password -batch -T "$Cmd"

    Write-Host "$Level"
}

function Get-Brightness {

    $Cmd = "cat $BrightFile"

    $Output = & $Plink -ssh "$User@$KindleIP" -pw $Password -batch -T $Cmd

    $KindleValue = [int]$Output

    # Convert Kindle 0-2399 -> 0-10
    $Level = [math]::Round($KindleValue / 239.9)

    Write-Host "$Level"
}

switch ($Action.ToLower()) {

    "set" {
        if (-not $PSBoundParameters.ContainsKey("Level")) {
            Write-Host "Usage: .\Kindle_Brightness.ps1 set <0-10>"
            exit
        }

        Set-Brightness -Level $Level
    }

    "get" {
        Get-Brightness
    }

    default {
        Write-Host "Unknown action: $Action"
        Write-Host "Usage:"
        Write-Host "  .\Kindle_Brightness.ps1 get"
        Write-Host "  .\Kindle_Brightness.ps1 set <0-10>"
    }
}
