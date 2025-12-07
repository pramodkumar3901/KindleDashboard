param(
	[Parameter(Mandatory = $true)]
    [string]$KindleIP
)
$User      = "root"
$Password  = "K1NdI3"
$Cmd       = "lipc-get-prop com.lab126.powerd battLevel"

$Plink = "plink"

& $Plink -ssh "$User@$KindleIP" -pw $Password -batch $Cmd
