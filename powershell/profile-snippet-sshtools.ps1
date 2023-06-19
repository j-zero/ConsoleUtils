using namespace System.Management.Automation

Register-ArgumentCompleter -CommandName ssh,scp,sftp -Native -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    ssh-find-bookmark($wordToComplete)
}

function _Get-SSH-Host($sshConfigPath) {
    Get-Content -Path $sshConfigPath `
    | Select-String -Pattern '^Host ' `
    | ForEach-Object { $_ -replace 'Host ', '' } `
    | ForEach-Object { $_ -split ' ' } `
    | Sort-Object -Unique `
    | Select-String -Pattern '^.*[*!?].*$' -NotMatch
}

function Get-SSH-Hosts(){
    $sshPath = "$env:USERPROFILE\.ssh"
    $hosts = Get-Content -Path "$sshPath\config" `
    | Select-String -Pattern '^Include ' `
    | ForEach-Object { $_ -replace 'Include ', '' }  `
    | ForEach-Object { Get-SSHHost "$sshPath/$_" }
    $hosts += _Get-SSH-Host("$sshPath\config")
    $hosts = $hosts | Sort-Object -Unique
    $hosts
}

function ssh-find-known-host([string]$wordToComplete) {
    $knownHosts = Get-Content ${Env:HOMEPATH}\.ssh\known_hosts `
    | ForEach-Object { ([string]$_).Split(' ')[0] } `
    | ForEach-Object { $_.Split(',') } `
    | Sort-Object -Unique `
    | where { $_ -match "^[a-zA-Z]" } # only DNS

    # For now just assume it's a hostname.
    $textToComplete = $wordToComplete
    $generateCompletionText = {
        param($x)
        $x
    }
    if ($wordToComplete -match "^(?<user>[-\w/\\]+)@(?<host>[-.\w]+)$") {
        $textToComplete = $Matches["host"]
        $generateCompletionText = {
            param($hostname)
            $Matches["user"] + "@" + $hostname
        }
    }

    $knownHosts `
    | Where-Object { $_ -like "${textToComplete}*" } `
    | ForEach-Object { [CompletionResult]::new((&$generateCompletionText($_)), $_, [CompletionResultType]::ParameterValue, $_) }
}

function ssh-copy-id([string]$pubkey, [string]$server)
{
    #ssh-save-bookmark($server)
    #ssh-find-known-host($server)
    Get-Content $env:USERPROFILE\.ssh\$pubkey | ssh $server "mkdir ~/.ssh/ && cat >> ~/.ssh/authorized_keys"
}

Register-ArgumentCompleter -CommandName 'ssh-copy-id' -ParameterName 'pubkey' -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameter)
    Get-ChildItem -Path "${Env:HOMEPATH}\.ssh\$wordToComplete*.pub" | Select-Object -ExpandProperty Name
}

Register-ArgumentCompleter -CommandName 'ssh-copy-id' -ParameterName 'server' -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameter)
    ssh-find-bookmark($wordToComplete)
    #ssh-find-known-host($wordToComplete)
    
    #"${Env:UserName}@"
}

function ssh-save-bookmark([string]$server){
    $path = "$env:LOCALAPPDATA/.ssh_bookmarks"
    #New-Item -ItemType Directory -Force -Path $env:USERPROFILE\.ssh_bookmarks -ErrorAction "silentcontinue"
    if(!(Test-Path $path) -or !(Select-String -Path $path -pattern $server -SimpleMatch)){
        Add-Content $path $server
    }
}

function ssh-find-bookmark([string]$needle){
    
    $path = "$env:LOCALAPPDATA/.ssh_bookmarks"
    $hosts = @()

    $needle_user = ""
    $needle_host = $needle

    if($needle -match "^((?<username>.*?)@)?(?<hostname>.*?)$"){
        $needle_user = $Matches.username
        $needle_host = $Matches.hostname
    }

    #$known_hosts = Get-Content -Path $path

    $known_hosts = Get-Content ${Env:HOMEPATH}\.ssh\known_hosts `
    | ForEach-Object { ([string]$_).Split(' ')[0] } `
    | ForEach-Object { $_.Split(',') } `
    | Sort-Object -Unique `
    | where { $_ -match "^[a-zA-Z]" } # only DNS

    $known_hosts | ForEach-Object {

        if ($_ -match "^(?<username>.*?)@(?<hostname>" + $needle_host +".*?)$") 
        {
            if($null -ne $needle_user){
                $hosts += $needle_user + "@" + $Matches.hostname
            }
            else{
                $hosts += $Matches.hostname
            }
        }
        elseif ((!($_ -match "@")) -and ($_ -match "^(?<hostname>" + $needle_host +".*?)$")) 
        {
            if($null -ne $needle_user){
                $hosts += $needle_user + "@" + $Matches.hostname
            }
            else{
                $hosts += $Matches.hostname
            }
        }
    }
    #[array]::Reverse($hosts)
    $hosts
}

