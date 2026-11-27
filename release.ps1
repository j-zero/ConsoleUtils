<#
.SYNOPSIS
    Creates a release: checks, tests, tags. The GitHub workflow builds and publishes it when the tag is pushed.

.EXAMPLE
    .\release.ps1 0.11.0          # check and create the tag v0.11.0 locally
    .\release.ps1 0.11.0 -Push    # ... and push the branch and the tag (starts the release build)
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidatePattern('^v?\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$')]
    [string]$Version,

    [switch]$Push,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Invoke-Git { & git @args; if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') failed" } }

$tag = if ($Version.StartsWith('v')) { $Version } else { "v$Version" }

$branch = (git rev-parse --abbrev-ref HEAD).Trim()
if ($branch -ne 'main') { throw "Releases are made from main, you are on '$branch'." }

if (git status --porcelain) { throw 'The working tree is not clean. Commit or stash your changes first.' }

if (git tag --list $tag) { throw "The tag $tag already exists." }

$last = (git describe --tags --abbrev=0 2>$null)
if ($last) {
    $new = [version]($tag.TrimStart('v') -replace '-.*$', '')
    $old = [version]($last.TrimStart('v') -replace '-.*$', '')
    if ($new -le $old) { throw "$tag is not newer than the last release $last." }
    Write-Host "Last release: $last"
}

if (-not $SkipTests) {
    Write-Host 'Building and testing ...'
    Push-Location (Join-Path $PSScriptRoot 'ConsoleUtils')
    try {
        dotnet build ConsoleUtils.sln --configuration Release -p:Platform=x64 -v:q -nologo
        if ($LASTEXITCODE -ne 0) { throw 'The build failed.' }
        dotnet test cross.core.tests --configuration Release --no-build -nologo
        if ($LASTEXITCODE -ne 0) { throw 'The tests failed.' }
    }
    finally { Pop-Location }
}

Invoke-Git tag -a $tag -m "ConsoleUtils $tag"
Write-Host "Created the tag $tag." -ForegroundColor Green

if ($Push) {
    Invoke-Git push origin main
    Invoke-Git push origin $tag
    Write-Host "Pushed. The release is built by GitHub Actions: https://github.com/j-zero/ConsoleUtils/actions" -ForegroundColor Green
}
else {
    Write-Host "Not pushed yet. To publish it run:  git push origin main $tag"
}
