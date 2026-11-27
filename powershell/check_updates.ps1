# Checks if a new ConsoleUtils release is available.
# Returns $true if there is one. The version comparison is done by consoleutils.update (it reads the VERSION file
# next to the programs), so there is no version number to maintain in this script.
#
#   . .\check_updates.ps1
#   if (Test-ConsoleUtilsUpdate) { consoleutils.update upgrade }

function Test-ConsoleUtilsUpdate {
    [CmdletBinding()]
    param()

    $updater = Get-Command consoleutils.update -ErrorAction SilentlyContinue
    if (-not $updater) {
        $local = Join-Path $PSScriptRoot '..\bin\consoleutils.update.exe'
        if (Test-Path $local) { $updater = Get-Item $local }
    }
    if (-not $updater) {
        Write-Error 'consoleutils.update was not found. Add the ConsoleUtils bin folder to your PATH.'
        return $false
    }

    $path = if ($updater -is [System.Management.Automation.ApplicationInfo]) { $updater.Source } else { $updater.FullName }
    & $path check --quiet
    # exit code: 0 = up to date, 10 = update available, 1 = error
    switch ($LASTEXITCODE) {
        0  { return $false }
        10 { return $true }
        default { Write-Error "consoleutils.update check failed (exit code $LASTEXITCODE)."; return $false }
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    Test-ConsoleUtilsUpdate
}
