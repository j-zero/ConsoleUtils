# change alias to new ls
Remove-Alias ls
New-Alias -Name 'ls' -Value 'list'

# powershell ssh tools
. "$PSScriptRoot\profile-snippet-sshtools.ps1"