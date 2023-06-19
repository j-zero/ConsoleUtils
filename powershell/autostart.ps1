Set-PSReadlineKeyHandler -Key Tab -Function MenuComplete

if(Get-Command "list" -erroraction 'silentlycontinue'){
    # change alias to new ls
    Remove-Alias ls
    New-Alias -Name 'ls' -Value 'list'
}

function mk([string]$new_dir){
    mkdir $new_dir
    Set-Location $new_dir
}

# powershell ssh tools
. "$PSScriptRoot\profile-snippet-sshtools.ps1"