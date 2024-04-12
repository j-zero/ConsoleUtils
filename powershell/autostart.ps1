Set-PSReadlineKeyHandler -Key Tab -Function MenuComplete

if(Get-Command "list" -erroraction 'silentlycontinue'){
    # change alias to new ls
    Remove-Alias ls
    New-Alias -Name 'ls' -Value 'list'
}

function mkd([string]$new_dir){
    mkdir $new_dir
    Set-Location $new_dir
}

function rdp([string]$server){
    mstsc.exe /prompt /v:$server
}

# powershell ssh tools
. "$PSScriptRoot\profile-snippet-sshtools.ps1"


#if(Get-Command "consoleutils.update" -erroraction 'silentlycontinue'){
#    consoleutils.update check
#}