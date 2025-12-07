param(
	[Parameter(Mandatory = $true)]
    [string]$KindleIP,    # "set" or "get"	
)
$User      = "root"
$Password  = "K1NdI3"
$Cmd       = "ls -l /mnt/us"

$Plink = "plink"

& $Plink -ssh "$User@$KindleIP" -pw $Password -batch $Cmd
