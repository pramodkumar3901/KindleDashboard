param(
	[Parameter(Mandatory = $true)]
    [string]$KindleIP
)
$User      = "root"
$Password  = "K1NdI3"
$Cmd       = "cat documents/StatusDumper/status.json"

$Plink = "plink"

& $Plink -ssh "$User@$KindleIP" -pw $Password -batch $Cmd
