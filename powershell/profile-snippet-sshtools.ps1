using namespace System.Management.Automation

Register-ArgumentCompleter -CommandName ssh,scp,sftp -Native -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    find-ssh-bookmark($wordToComplete)
}

function find-ssh-known-host([string]$wordToComplete) {
    $knownHosts = Get-Content ${Env:HOMEPATH}\.ssh\known_hosts `
    | ForEach-Object { ([string]$_).Split(' ')[0] } `
    | ForEach-Object { $_.Split(',') } `
    | Sort-Object -Unique

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
    save-ssh-bookmark($server)
    Get-Content $env:USERPROFILE\.ssh\$pubkey | ssh $server "mkdir ~/.ssh/ && cat >> ~/.ssh/authorized_keys"
}

Register-ArgumentCompleter -CommandName 'ssh-copy-id' -ParameterName 'pubkey' -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameter)
    Get-ChildItem -Path "${Env:HOMEPATH}\.ssh\$wordToComplete*.pub" | Select-Object -ExpandProperty Name
}

Register-ArgumentCompleter -CommandName 'ssh-copy-id' -ParameterName 'server' -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameter)
    find-ssh-bookmark($wordToComplete)
    
    #"${Env:UserName}@"
}

function save-ssh-bookmark([string]$server){
    $path = "$env:LOCALAPPDATA/.ssh_bookmarks"
    #New-Item -ItemType Directory -Force -Path $env:USERPROFILE\.ssh_bookmarks -ErrorAction "silentcontinue"
    if(!(Test-Path $path) -or !(Select-String -Path $path -pattern $server -SimpleMatch)){
        Add-Content $path $server
    }
}

function find-ssh-bookmark([string]$needle){
    
    $path = "$env:LOCALAPPDATA/.ssh_bookmarks"
    $hosts = @()

    $needle_user = ""
    $needle_host = $needle

    if($needle -match "^((?<username>.*?)@)?(?<hostname>.*?)$"){
        $needle_user = $Matches.username
        $needle_host = $Matches.hostname
    }


    Get-Content -Path $path | ForEach-Object {
        if ($_ -match "^((?<username>" + $needle_user + ".*?)@)?(?<hostname>" + $needle_host +".*?)$") 
        {
            if($null -ne $Matches.username){
                $hosts += $Matches.username + "@" + $Matches.hostname
            }
            else{
                $hosts += $Matches.hostname
            }
        }
    }
    [array]::Reverse($hosts)
    $hosts
}

#save-ssh-bookmark("ringej@otp1.mh-hannover.local")

