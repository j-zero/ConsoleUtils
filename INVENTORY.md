# ConsoleUtils – Inventory

Stand: 2026-11-27 · Target: **.NET 10** (`net10.0` / `net10.0-windows`) · 35 projects (31 executables, 4 libraries)

All projects build into the shared `bin\` folder at the repository root (`OutputPath = ..\..\bin\`),
the solution is `ConsoleUtils\ConsoleUtils.sln` (solution folders: *Libraries*, *Tools*, *Prototypes*).

## Repository layout

| Path | Content |
|---|---|
| `ConsoleUtils/` | solution `ConsoleUtils.sln` and one folder per project |
| `ConsoleUtils/build.cmd`, `build.sh` | local build helpers |
| `powershell/` | PowerShell helpers (profile snippets, autostart, update check) |
| `.github/workflows/build.yml` | CI: build (Windows), artifact `Kompilat`, release zip on `v*` tags |
| `release.cmd`, `write-version.cmd` | write the `VERSION` file from `git describe --tags` |
| `bin/` | build output (not versioned) |

## Tools (alphabetical)

"Core" says which shared library a tool uses. *cross* = `cross.core`, *win* = `windows.core`, *legacy* = `leviathan.core`.
"Platform": **any** = builds for plain `net10.0`, **win** = `net10.0-windows` (Windows only).

| Executable | Folder / project | Purpose | Platform | Core | LOC |
|---|---|---|---|---|---:|
| `bits` | `bits/bits.csproj` | convert numbers / show bits | any | cross | 628 |
| `certy` | `certy/certy.csproj` | show the TLS certificate (and chain) of `host:port` | any | cross | 700 |
| `chaos` | `chaos/chaos.csproj` | shuffle, sort and de-duplicate lines of text | any | cross | 322 |
| `coffee` | `coffee/coffee.csproj` | keep the system awake, optionally while running a command | win | cross + win | 196 |
| `consoleutils.update` | `consoleutils.update/` | self-updater, downloads the latest GitHub release | win | – | 756 |
| `d` | `d/d.csproj` | date/time overview: week, day of year, UTC, Unix time, Swatch beats, calendar | any | cross | 77 |
| `download` | `download/download.csproj` | download a URL, optionally show headers | win | legacy | 328 |
| `ed` | `ed/ed.csproj` | interactive line viewer/editor for text files (work in progress) | win | legacy | 276 |
| `events` | `events/events.csproj` | read / export the Windows Event Log (XML, append) | win | own copies | 1788 |
| `file` | `file/file.csproj` | identify file type: magic description, MIME type, encoding | win | legacy | 105 |
| `free` | `free/free.csproj` | CPU, RAM and drive usage | win | legacy | 169 |
| `google` | `google/google.csproj` | web search from the command line | win | – | 82 |
| `gremlins` | `gremlins/gremlins.csproj` | show strange / invisible characters in text files | any | cross | 558 |
| `hash` | `hash/hash.csproj` | checksums and hashes (CRC16/32/64, ELF32, MD5, SHA1, SHA256 …) | win | legacy | 185 |
| `hekate` | `hekate/hekate.csproj` | Windows Credential Manager utility | win | legacy | 661 |
| `hexe` | `hexe/hexe.csproj` | hex viewer | any | cross | 1013 |
| `img` | `img/img.csproj` | render an image in the console | win | own copy | 544 |
| `kb` | `klemmbrett_old/klemmbrett.csproj` | console clipboard utility (old, working) | win | – | 1520 |
| `kb2` | `klemmbrett/klemmbrett2.csproj` | rewrite of `kb` (does not work yet) | win | legacy | 352 |
| `list` | `l/list.csproj` | `ls`-like file listing with owner, attributes, MIME info, ADS | win | legacy | 867 |
| `log` | `lognote/lognote.csproj` | note taking / log book in `Documents\LogNote` | win | legacy | 472 |
| `mk` | `touch/mk.csproj` | `touch` – create files | any | – | 83 |
| `open` | `open/open.csproj` | open a file/folder/URL with the default application | win | – | 151 |
| `path` | `path/path.csproj` | print the full path and copy it to the clipboard | win | – | 86 |
| `rip` | `recycle/trash.csproj` | move files to the recycle bin instead of deleting them | win | – | 312 |
| `rmline` | `rmline/rmline.csproj` | remove a line from a file (prototype, hard-coded paths) | any | – | 25 |
| `seek` | `seek/seek.csproj` | search files with Windows Search | win | legacy | 168 |
| `subnet` | `subnet/subnet.csproj` | subnet calculator | any | cross | 456 |
| `sudo` | `sudo/sudo.csproj` | run a command elevated | win | legacy | 163 |
| `tulpn` | `tulpn/tulpn.csproj` | list listening TCP ports with the owning process command line | win | legacy | 249 |
| `unpack` | `unpack/unpack.csproj` | list / extract archives (7-Zip) | win | legacy + SevenZipExtractor | 242 |
| `wiki` | `wiki/wiki.csproj` | stub ("Hello, Wiki!") | any | – | 2 |

## Libraries

| Library | Folder | Purpose | Platform | LOC |
|---|---|---|---|---:|
| `cross.core` | `cross.core/` | command line parser, console helpers, colors (Pastel), file definitions – platform independent | any | 2258 |
| `windows.core` | `windows.core/` | NTFS data streams, parent process, Windows helpers (`System.Management`) | win | 1029 |
| `leviathan.core` | `ConsoleUtilsCore/` | **legacy** core, the former .NET Framework library (file system entries, MIME, owner/ACL, Elf/CRC, …) | win | 6372 |
| `SevenZipExtractor` | `SevenZipExtractor/` | C# wrapper for `7z.dll` (x64 and x86 DLL included) | win (native DLL) | 1824 |

## NuGet packages in use

| Package | Used by |
|---|---|
| `Mime`, `MimeTypesMap` | `leviathan.core`, `list`, `open` |
| `System.Management` | `leviathan.core`, `windows.core`, `events`, `sudo`, `tulpn` |
| `System.Drawing.Common` | `leviathan.core`, `img` |
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

## Findings / technical debt

* **Three core libraries.** `leviathan.core` (legacy, ~6.4k LOC) duplicates much of `cross.core` + `windows.core`.
  13 tools still depend on `leviathan.core`: `download`, `ed`, `file`, `free`, `hash`, `hekate`, `kb2`, `list`, `log`, `seek`, `sudo`, `tulpn`, `unpack`.
  The natural next step is to move them to `cross.core` / `windows.core` and retire `leviathan.core`.
* **Private copies of core files** in `events` (ColorTheme, CommandLineParser, ConsoleExtensions, ParentProcessHelper), `img` (ConsoleExtensions),
  `klemmbrett_old` and `recycle` (each with its own `FileAndDirectoryFilter.cs`).
* **Folder names differ from the executable name:** `l`→`list`, `touch`→`mk`, `recycle`→`rip`, `lognote`→`log`, `klemmbrett`→`kb2`, `klemmbrett_old`→`kb`, `ConsoleUtilsCore`→`leviathan.core`.
* **Prototypes / stubs:** `wiki` (2 lines), `rmline` (hard-coded paths), `kb2` (not working), `ed` (work in progress).
* **Redirected output:** several tools read `Console.WindowWidth`, `CursorVisible` or `Clear()` unguarded and throw when stdout/stdin is redirected
  (`tulpn`, `ed`, `log`, …). `list` is already fixed.
* **`tulpn`** waits for `Console.ReadLine()` at the end, it cannot be used in pipelines.
* **`powershell/check_updates.ps1`** has the installed version hard-coded (`v0.5.0`) instead of reading `VERSION`.
* **`mk`** does not strip quotes around a single path argument.
* **CI:** `.github/workflows/build.yml` was updated to .NET 10 and v4 actions, but has not been run yet.
* **`bin/`** may contain stale files from the .NET Framework / .NET 8 builds (`*.dll.config`, `Debug/`, `Release/`, old 4.x packages).
  A clean rebuild after deleting `bin/` gives a tidy output folder.
