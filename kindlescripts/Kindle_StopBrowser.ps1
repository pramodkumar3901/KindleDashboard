param(
	[Parameter(Mandatory = $true)]
    [string]$KindleIP,    # "set" or "get"	
)
$User      = "root"
$Password  = "K1NdI3"

$Cmd       = "bash /documents/shortcut_stop.sh"

$Plink = "plink"

& $Plink -ssh "$User@$KindleIP" -pw $Password -batch $Cmd
