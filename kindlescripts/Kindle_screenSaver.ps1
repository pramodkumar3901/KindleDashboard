param(
	[Parameter(Mandatory = $true)]
    [string]$KindleIP    
)
$User      = "root"
$Password  = "K1NdI3"
$Cmd       = "lipc-get-prop com.lab126.powerd preventScreenSaver"

$Plink = "plink"

& $Plink -ssh "$User@$KindleIP" -pw $Password -batch $Cmd
