using namespace System.Management.Automation

Register-ArgumentCompleter -CommandName ssh,scp,sftp -Native -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
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
    type $env:USERPROFILE\.ssh\$pubkey | ssh $server "mkdir ~/.ssh/ && cat >> ~/.ssh/authorized_keys"
}

Register-ArgumentCompleter -CommandName 'ssh-copy-id' -ParameterName 'pubkey' -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameter)
    Get-ChildItem -Path "${Env:HOMEPATH}\.ssh\$wordToComplete*.pub" | Select-Object -ExpandProperty Name
}

Register-ArgumentCompleter -CommandName 'ssh-copy-id' -ParameterName 'server' -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameter)
    "${Env:UserName}@"
}

