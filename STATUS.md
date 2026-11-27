# ConsoleUtils – Status of the tools

Review of all tools: what they do, how finished they are, and what is left to do.
Based on reading the code and running every tool (with no arguments and with invalid input) on 2026-11-27.

Legend: **🟢 done** (does what it says, only polish left) · **🟡 usable** (works, but has known gaps or rough edges) · **🔴 half-finished** (core idea works only partly, or it is a prototype/stub)

## Overview

| Tool | State | One-line summary |
|---|:---:|---|
| `wiki` | 🔴 | stub, prints "Hello, Wiki!" |
| `ed` | 🔴 | parked: works as a pager, meant to become a simple console text editor |
| `rip` | 🔴 | recycle-bin tool with an argument-loop bug |
| `hash` | 🟡 | `--crc16` not implemented, `-f` ignored when stdin is redirected |
| `sudo` | 🟡 | elevated start works, no-argument mode drops the parent's arguments |
| `tulpn` | 🟡 | TCP listeners only, blocks at the end |
| `unpack` | 🟡 | list/extract works, no progress, wrong help text, crashes on unknown archives |
| `download` | 🟡 | works (with progress), no stdout mode, crashes on bad URL |
| `img` | 🟡 | works for images, crashes without/with wrong argument |
| `bits` | 🟡 | works, crashes without arguments, SI prefixes missing |
| `certy` | 🟡 | works, crashes on unreachable host |
| `seek` | 🟡 | works, swallows all errors, query built by string concatenation |
| `log` | 🟡 | works, error handling missing, small command set |
| `google` | 🟡 | works, search term is not URL-encoded |
| `mk`, `path`, `open` | 🟡 | work, but treat the whole command line as one path |
| `events` | 🟡 | works, pauses on exit, some parts experimental |
| `hexe` | 🟡 | very complete, open feature list in the code |
| `kb` | 🟡 | working clipboard tool, one TODO |
| `free` | 🟡 | RAM/drives work, **CPU usage is always 0 %** |
| `gremlins`, `chaos`, `coffee`, `d`, `file`, `list`, `hekate`, `subnet`, `rmline`, `kb2`, `consoleutils.update` | 🟢 | finished |

---

## 🔴 Half-finished

### `wiki` – stub
* **Does:** prints `Hello, Wiki!` (2 lines).
* **To do:** decide what it should be (wiki lookup? Wikipedia summary?). Otherwise remove the project from the solution.

### `ed` – line viewer / editor (parked)
* **Plan:** `ed` is meant to become a simple text editor for the console. It stays as it is for now; nothing is blocked by it.
* **Does:** shows a text file page-wise (arrows, PageUp/PageDown, space, `q`).
* **Missing:** the code for changing a line (`lineChanger`) and for writing a modified file exists but is never called; it ends with `TODO: Delete the old file and replace it with the new file here`. Crashes without an argument. Reads the whole file, no encoding handling.
* **To do:** decide *editor* or *pager*. If editor: cursor, insert/delete, save with atomic replace. If pager: remove the dead editing code and call it `view`/`pg`. In both cases: usage message, redirected-output check, `CursorVisible` restore on exit.

### `rip` – recycle bin
* **Does:** moves files to the recycle bin through the shell API (`FileOperationAPIWrapper`).
* **Bug:** `foreach (arg in args)` calls the filter with **all** `args` each time, so with N arguments every file is processed N times.
* **Missing:** no usage text, no confirmation/`-f`, no message about what was moved, no error code when nothing was found. Has its own `FileAndDirectoryFilter` copy with a different API than the one in `windows.core`.
* **To do:** fix the loop (expand each argument once, support wildcards via `windows.core`), print results, set the exit code.

---

## 🟡 Usable, but with gaps

### `hash`
* `--crc16` is listed in the help (`// TODO`) but has no implementation – it silently falls back to SHA1.
* The help says SHA256 is the default, the code uses SHA1.
* If stdin is redirected, stdin is hashed and the `-f file` argument is ignored.
* **To do:** implement CRC16 or remove the option, make the default match the help, give `--file` priority over stdin, add `--all`/several algorithms at once, `-c` check mode would be natural.

### `sudo`
* Starts a program with the `runas` verb – works.
* With no arguments it tries to re-run the parent process' command line elevated, but the parameters are taken from `args` (empty) instead of the parsed command line, so the parent's arguments are lost.
* `GetSystem()` (impersonating the `winlogon` token) is never called; `IsAdministrator()` is only used by it.
* **To do:** fix the no-argument mode, remove dead code, return the child's exit code (`UseShellExecute` + `runas` makes that hard – consider `-w`/wait), usage text.

### `tulpn`
* Lists listening **TCP** ports with the command line of the owning process.
* **Missing:** UDP, established connections (the code has a commented-out "remote" part), process name/PID column, filtering and sorting. Waits for `Console.ReadLine()` at the end, so it cannot be piped. Throws when stdout is redirected (`Console.WindowWidth`).
* **To do:** `--udp`, `--all`, `--pid`, drop the `ReadLine`, guard the console width.

### `unpack`
* Lists and extracts archives through `7z.dll`.
* **Rough edges:** the `extract` verb has the help text of `list` ("List entry in archive."); progress percentage is a `todo`; `GetExtension` cuts at the *first* dot (`a.tar.gz` → `.tar.gz`, colors wrong for `my.file.txt`); unknown archive types end in an unhandled `SevenZipException`; no password option (encrypted archives fail), no "extract single file", no `create`.
* **To do:** fix help/extension, catch errors with a readable message, progress bar, `--password`, `--include` pattern.

### `download`
* HTTP download with proxy/timeout/header options.
* **Missing:** the `TODO Output to STDOUT instead File Download`; a malformed URL ends in `UriFormatException`; no resume of interrupted downloads. Uses the obsolete `WebClient`.
* **To do:** stdout mode (`-O -`), URL validation, `HttpClient` instead of `WebClient`, resume.

### `img`
* Renders an image with background-colored cells, scaled to the console. Uses `System.Drawing` (Windows only, GDI+).
* **Missing:** crashes without an argument or with a missing/unsupported file; no usage text; no option for width/height or for half-block rendering (2× vertical resolution); no animated GIFs.
* **To do:** argument handling, `--width/--height`, consider `ImageSharp` to become cross-platform.

### `free`
* Shows CPU, RAM and drive usage.
* **Bug:** `PerformanceCounter.NextValue()` always returns `0` on the first call. The CPU value is read once, so it always prints `0%`.
* **To do:** call `NextValue()`, wait ~500 ms, call it again (or `--watch` for continuous output). The tool also creates the counters even when only drives are wanted, which costs startup time.

### `bits`
* Converts numbers/bits. `NullReferenceException` without arguments; the source has `TODO SI niut prefixes`.
* **To do:** usage text for empty input, SI/IEC prefix handling.

### `certy`
* Shows TLS certificate, chain and PEM of `host:port`.
* **Missing:** an unreachable host or a handshake error ends in an unhandled `AggregateException`.
* **To do:** catch connection/TLS errors with a readable message and a non-zero exit code, `--timeout`, `--sni`.

### `seek`
* Searches the Windows Search index (`SystemIndex` through OleDb).
* **Problems:** the whole `Main` is wrapped in an empty `catch {}` – errors vanish silently; the SQL query is built by string concatenation (a `'` in the search term or directory breaks it); no default directory handling; commented-out alternatives for folder-only search.
* **To do:** parameterise/escape the query, report errors, add `--dirs-only`, default to the current directory, fall back to a normal file system search if the index is off.

### `log` (lognote)
* Interactive note taking with themes, history, auto-completion and clipboard/image import.
* **Missing:** four `todo error handling` markers (file writes); startup argument handling is commented out (`ParseLine(f)`), so it cannot be used as `log "my note"`; few commands (`:open`, `:image`, `:clear`, `:list`, `:quit`); no search, no export.
* **To do:** one-shot mode with arguments, error handling, `:search`/`:export`.

### `google`
* Opens a Google search in the browser.
* **Problem:** the search term is joined with `+` but not URL-encoded (`&`, `#`, `%`, umlauts break it). Hard-wired to Google.
* **To do:** `Uri.EscapeDataString`, optional `--engine`.

### `mk`, `path`, `open`
* `mk` (touch), `path` (print + copy full path), `open` (open with default app) all take **the raw command line as one path**: `mk a.txt b.txt` creates a single file called `a.txt b.txt`; quoted paths keep their quotes (`mk "a b.txt"` fails).
* `path` used to crash with an `ArgumentNullException` for a path that does not exist (empty clipboard text); it now prints `Not found:` and exits with code 1.
* **To do:** accept several arguments, strip quotes. `mk` should also support `-d` for directories and `-t` for a timestamp.

### `events`
* Reads/exports the Windows event log live, with XML output and logging into a file.
* **Rough edges:** waits for Enter on exit (`Loader.Main` → `Console.ReadLine()`); `--colored-log` is marked experimental; `Console_CancelKeyPress` still has a commented-out `NotImplementedException`; tool-specific palette `EventColors`.
* **To do:** remove the pause when output is redirected, finish or drop the colored log.

### `hexe`
* The most complete tool (hex/binary/string view, search, cut, dump, paging). Open points are listed in the source:
  `cut to binary, patch, remove, skip` (header), read all offsets first and then stream (`313`), alternate data streams on Windows (`521`), line numbers + gremlins (`728`), the `hybrid` mode is marked experimental.
* **To do:** patch/replace bytes, skip/remove ranges, ADS display via `windows.core`.

### `kb`
* Complete console clipboard tool (copy, paste, files, text, html, rtf, image, save/load, …).
* **Open:** one `TODO, copy plaintext from html`. Contains its own `FileAndDirectoryFilter`, `CopyDialog`, … copies instead of the shared libraries.

---

## 🟢 Finished (only polish)

| Tool | Notes |
|---|---|
| `gremlins` | complete. `Exit()` (pause when started from Explorer) is commented out with a `Todo Windows/Linux`. |
| `chaos` | complete (shuffle, sort, uniq, count, case, encoding). |
| `coffee` | complete (keep awake, optional command, always-on-top). |
| `d` | complete, tiny. Date/time/calendar overview. |
| `file` | complete. |
| `list` | complete; fixed in this session (redirected output, empty directories, owner parsing, 10× faster). `-i` loads assemblies with `Assembly.LoadFrom` to read the .NET version – better done with `System.Reflection.Metadata`. |
| `hekate` | complete credential manager tool. `GetWifiCreds.cs` is a separate stand-alone program with its own `Main`; it is **not compiled** (excluded in `hekate.csproj`) and is not part of the tool. |
| `subnet` | complete. |
| `kb2` | complete clipboard tool, successor of `kb`: show/info/get, copy/cut/paste/move of files (Explorer compatible cut/copy flag), text/html/rtf/csv/image from file or stdin, save/load, raw/dump of any format, clear. Wildcards, `--force`, `--quiet`, exit codes. `kb` can be removed once `kb2` is accepted (only `kb remove` is not carried over, `rip` does that). |
| `rmline` | complete: `rmline file.ext:LINE [...]` deletes the line(s), keeps encoding/BOM and line endings, replaces the file atomically, exit code 1 on errors. Also accepts `~/` paths and several lines per file. |
| `consoleutils.update` | rewritten: GitHub API instead of a VERSION download, SHA-256 verification, zip-slip protection, files in use are renamed, rollback on errors, `check` with exit codes for scripts, `--yes`/`--force`. Tested against a local fake server (success, wrong checksum, zip slip, missing checksum, failure with rollback); not yet against a real release made by the new workflow. |

---

## Cross-cutting work (all tools)

1. **Redirected input/output.** `Console.WindowWidth`, `CursorVisible`, `Clear()`, `ReadKey()` throw when stdout/stdin is redirected (`tulpn`, `ed`, `log`, `hash`, …). A shared helper (`ConsoleHelper.GetWidth()`, `IsInteractive`) in `cross.core` would fix all of them – `list` has a local version already.
2. **"Press any key" when started from Explorer** is copy-pasted into ~10 tools (parent-process check + `ReadKey`). Move it into one helper in `windows.core`.
3. **Usage on bad input.** `bits`, `img`, `ed`, `download`, `certy`, `unpack` end in stack traces for missing or wrong arguments. A common `try/catch` in the shared `Main` pattern with a readable message and an exit code would unify this.
4. **Exit codes.** Most tools return 0 even on errors (`seek`, `rip`, `hash`).
5. **Tests.** Only `cross.core.tests` exists (10 tests for `AnsiText`). The pure logic (`subnet`, `bits`, `hash`, `chaos`, `CmdParser`, `FileAndDirectoryFilter`, `UnitHelper`) is easy to cover with xUnit.
6. **Tool-specific copies** of shared code: `kb` and `rip` (own `FileAndDirectoryFilter`), `events` (`EventColors` palette, own `Loader`).
7. **Measuring colored text.** Use `AnsiText` from `cross.core` (`VisibleLength`, `PadRightVisible`, `TruncateVisible`, `Strip`) instead of `string.Length`/`PadRight` on strings that went through `.Pastel()`. `list`, `unpack` and `tulpn` still pad before coloring and could use it.
