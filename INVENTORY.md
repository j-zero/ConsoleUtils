# ConsoleUtils – Inventory

Stand: 2026-11-27 · Target: **.NET 10** (`net10.0` / `net10.0-windows`) · 34 projects (31 executables, 3 libraries)

How finished each tool is, and what is left to do, is in [STATUS.md](STATUS.md).

## Repository layout

| Path | Content |
|---|---|
| `ConsoleUtils/` | solution `ConsoleUtils.sln`, one folder per project, shared build files |
| `ConsoleUtils/Directory.Build.props` | settings shared by all projects (output folder `bin\`, no assembly info, unsafe blocks) |
| `ConsoleUtils/Directory.Packages.props` | central NuGet package versions |
| `ConsoleUtils/build.cmd`, `build.sh` | local build helpers |
| `powershell/` | PowerShell helpers (profile snippets, autostart, update check) |
| `.github/workflows/build.yml` | CI: build (Windows), artifact `Kompilat`, release zip on `v*` tags |
| `release.cmd`, `write-version.cmd` | write the `VERSION` file from `git describe --tags` |
| `bin/` | build output (not versioned, every project builds into it) |

Every project folder has the name of its executable, the project file has the same name (`list/list.csproj` → `list.exe`).
The solution is organised in the solution folders *Libraries*, *Tools* and *Prototypes*.

## Tools (alphabetical)

"Platform": **any** = builds for plain `net10.0`, **win** = `net10.0-windows` (Windows only).
"Uses": `cross` = `cross.core`, `win` = `windows.core`.

| Executable | Purpose | Platform | Uses | LOC |
|---|---|---|---|---:|
| `bits` | convert numbers / show bits | any | cross | 628 |
| `certy` | show the TLS certificate (and chain) of `host:port` | any | cross | 700 |
| `chaos` | shuffle, sort and de-duplicate lines of text | any | cross | 322 |
| `coffee` | keep the system awake, optionally while running a command | win | cross, win | 196 |
| `consoleutils.update` | self-updater, downloads the latest GitHub release | win | cross | 508 |
| `d` | date/time overview: week, day of year, UTC, Unix time, Swatch beats, calendar | any | cross | 77 |
| `download` | download a URL, optionally show headers | win | cross, win | 328 |
| `ed` | interactive line viewer for text files (editing not finished) | win | cross, win | 276 |
| `events` | read / export the Windows Event Log (XML, append) | win | cross, win | 620 |
| `file` | identify file type: magic description, MIME type, encoding | win | cross, win | 105 |
| `free` | CPU, RAM and drive usage | win | cross, win | 169 |
| `google` | web search from the command line | any | – | 82 |
| `gremlins` | show strange / invisible characters in text files | any | cross | 558 |
| `hash` | checksums and hashes (CRC32/64, ELF32, MD5, SHA1, SHA256 …) | win | cross, win | 185 |
| `hekate` | Windows Credential Manager utility | win | cross, win | 661 |
| `hexe` | hex viewer | any | cross | 1013 |
| `img` | render an image in the console | win | cross | 150 |
| `kb` | console clipboard utility (working) | win | – | 1520 |
| `kb2` | rewrite of `kb` (dumps clipboard formats only) | win | cross, win | 352 |
| `list` | `ls`-like file listing with owner, attributes, MIME info, ADS | win | cross, win | 867 |
| `log` | note taking / log book in `Documents\LogNote` | win | cross, win | 472 |
| `mk` | `touch` – create files | any | – | 83 |
| `open` | open a file/folder/URL with the default application | win | – | 151 |
| `path` | print the full path and copy it to the clipboard | win | – | 86 |
| `rip` | move files to the recycle bin instead of deleting them | win | – | 312 |
| `rmline` | remove a line from a file (prototype) | any | – | 25 |
| `seek` | search files with Windows Search | win | cross, win | 168 |
| `subnet` | subnet calculator | any | cross | 456 |
| `sudo` | run a command elevated | win | cross, win | 163 |
| `tulpn` | list listening TCP ports with the owning process command line | win | cross, win | 249 |
| `unpack` | list / extract archives (7-Zip) | win | cross, win, SevenZipExtractor | 242 |
| `wiki` | stub ("Hello, Wiki!") | any | – | 2 |

## Libraries

| Library | Purpose | Platform | LOC |
|---|---|---|---:|
| `cross.core` | command line parser, console helpers and colors (Pastel), hashes/CRC (`Crc32/64`, `Elf32`), `Base58`, MIME detection, `ReadLine`, file type definitions, encoding/convert/string/unit helpers | any | 4326 |
| `windows.core` | file system entries with owner/ACL/links/ADS (`FilesystemEntryInfo`), wildcard file filter, network shares, NTFS data streams, parent process, `PathHelper`, Windows helpers | win | 2162 |
| `SevenZipExtractor` | C# wrapper for `7z.dll` (x64 and x86 DLL included) | win (native DLL) | 1824 |

`windows.core` references `cross.core`; tools that reference `windows.core` get `cross.core` through it, but list both explicitly.
The former .NET Framework library `leviathan.core` was split into these two and removed.

## NuGet packages

Versions are defined once in `ConsoleUtils/Directory.Packages.props`.

| Package | Used by |
|---|---|
| `Mime`, `MimeTypesMap` | `cross.core` (MIME detection), `open` |
| `System.Management` | `windows.core` |
| `System.Drawing.Common` | `img` |
| `System.Diagnostics.EventLog` | `events` |
| `System.Diagnostics.PerformanceCounter` | `free` |
| `System.Security.Cryptography.ProtectedData` | `hekate` |
| `System.Data.OleDb` | `seek` |
| `TimeZoneNames` | `d` |

## PowerShell helpers (`powershell/`)

| File | Purpose |
|---|---|
| `autostart.ps1` | profile setup (dot-source from `$PROFILE`): tab menu completion, replaces the `ls` alias with `list`, small helper functions such as `mkd` |
| `check_updates.ps1` | compares a hard-coded version (`v0.5.0`) with the latest GitHub release |
| `profile-snippet-sshtools.ps1` | tab completion for `ssh`/`scp`/`sftp` from `~/.ssh/config`, SSH bookmark helpers (`ssh-copy-id`, …) |

## Remaining technical debt

* **Tool-specific copies of shared code:** `kb` and `rip` each have their own `FileAndDirectoryFilter.cs` (different API than the one in `windows.core`),
  `kb` also has its own clipboard/HTML helpers; `events` has its own colour palette `EventColors`.
* **"Press any key" when started from Explorer** is copy-pasted into about ten tools (parent process check + `ReadKey`).
* **Redirected output:** several tools read `Console.WindowWidth`, `CursorVisible` or `Clear()` unguarded and throw when stdout/stdin is redirected (`tulpn`, `ed`, `log`, …). `list` is already fixed.
* **Command line handling:** `mk`, `path` and `open` use the raw command line as one path instead of the parsed arguments.
* **`powershell/check_updates.ps1`** has the installed version hard-coded instead of reading `VERSION`.
* **No tests.**
* **CI:** `.github/workflows/build.yml` was updated to .NET 10 and v4 actions, but has not been run yet.
