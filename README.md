# ConsoleUtils

Collection of small console utilities for Windows (.NET 10). Some may not work, or not work for you.

* [INVENTORY.md](INVENTORY.md) – all projects, libraries, dependencies and technical debt
* [STATUS.md](STATUS.md) – how finished each tool is and what is left to do

## Build

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
cd ConsoleUtils
dotnet build ConsoleUtils.sln --configuration Release -p:Platform=x64
```

All executables end up in `bin\` in the repository root. Add that folder to your `PATH`.
The PowerShell helpers in [`powershell/`](powershell/README.md) can be loaded from your `$PROFILE`.

## Updating

`consoleutils.update` installs the newest release from GitHub:

```powershell
consoleutils.update            # check and ask
consoleutils.update check      # only check, exit code 10 = update available (for scripts)
consoleutils.update upgrade -y # install without asking
```

The download is verified with its SHA-256 checksum, files that are in use are renamed and replaced, and everything is
restored if the update fails. See `consoleutils.update help`.

## Releases

A release is made from `main` with a tag. The GitHub workflow builds, tests and publishes
`ConsoleUtils.zip`, `ConsoleUtils.zip.sha256` and `VERSION`:

```powershell
.\release.ps1 0.15.0          # checks, builds, tests and creates the tag v0.15.0
.\release.ps1 0.15.0 -Push    # ... and pushes branch and tag, which starts the release build
```

## Tools

| Tool | Description |
|---|---|
| `bits` | convert numbers / show bits |
| `certy` | show the TLS certificate (and chain) of a host |
| `chaos` | shuffle, sort and de-duplicate lines of text |
| `coffee` | keep the system awake, optionally while running a command |
| `consoleutils.update` | update ConsoleUtils from the latest GitHub release |
| `d` | date, time, week number, Unix time and a calendar |
| `download` | download a URL, optionally show headers |
| `ed` | line viewer, planned as a simple console editor (parked) |
| `events` | read and export the Windows Event Log |
| `file` | identify file types (magic, MIME type, encoding) |
| `free` | CPU, RAM and drive usage |
| `google` | google from the command line |
| `gremlins` | show strange characters in text files |
| `hash` | checksums and hashes (CRC, MD5, SHA, …) |
| `hekate` | Windows Credential Manager utility |
| `hexe` | simple hex viewer |
| `img` | show an image in the console |
| `kb` | console clipboard utility (old, superseded by `kb2`) |
| `kb2` | clipboard for the console: text, html, rtf, csv, images, files, raw formats |
| `list` | `ls`-like file listing |
| `log` | note taking / log book |
| `mk` | create files (`touch`) |
| `open` | open a file/folder/URL with the default application |
| `path` | print the full path and copy it to the clipboard |
| `rip` | put files in the recycle bin instead of deleting them |
| `rmline` | delete a line from a file: `rmline file.ext:LINE` |
| `seek` | search files with Windows Search |
| `subnet` | simple subnet calculator |
| `sudo` | run a command elevated |
| `tulpn` | list listening TCP ports and their processes |
| `unpack` | list and extract archives |
| `wiki` | stub |
