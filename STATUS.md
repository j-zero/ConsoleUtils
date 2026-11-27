# ConsoleUtils – Status of the tools

Review of all tools: what they do, how finished they are, and what is left to do.
Based on reading the code and running every tool (with no arguments and with invalid input) on 2026-11-27.

Legend: **🟢 done** (does what it says, only polish left) · **🟡 usable** (works, but has known gaps or rough edges) · **🔴 half-finished** (core idea works only partly, or it is a prototype/stub)

## Overview

| Tool | State | One-line summary |
|---|:---:|---|
| `wiki` | 🔴 | stub, prints "Hello, Wiki!" |
| `ed` | 🔴 | parked: works as a pager, meant to become a simple console text editor |
| `hash` | 🟡 | `--crc16` not implemented, `-f` ignored when stdin is redirected |
| `sudo` | 🟡 | elevated start works, no-argument mode drops the parent's arguments |
| `tulpn` | 🟡 | TCP listeners only |
| `unpack` | 🟡 | list/extract works, no progress, wrong help text, no password option |
| `download` | 🟡 | works (with progress), no stdout mode, no resume |
| `img` | 🟡 | works for images, no usage text, no size options |
| `bits` | 🟡 | works, SI prefixes missing |
| `certy` | 🟡 | works, no `--timeout`/`--sni` options |
| `seek` | 🟡 | works, swallows all errors, query built by string concatenation |
| `log` | 🟡 | works, error handling missing, small command set |
| `google` | 🟡 | works, search term is not URL-encoded |
| `mk`, `path`, `open` | 🟡 | work, but treat the whole command line as one path |
| `events` | 🟡 | works, pauses on exit, some parts experimental |
| `hexe` | 🟡 | very complete, open feature list in the code |
| `kb` | 🟡 | working clipboard tool, one TODO |
| `gremlins`, `chaos`, `coffee`, `d`, `file`, `free`, `list`, `hekate`, `subnet`, `rmline`, `rip`, `kb2`, `consoleutils.update` | 🟢 | finished |

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
* **Missing:** UDP, established connections (the code has a commented-out "remote" part), process name/PID column, filtering and sorting. (It no longer waits for Enter at the end and works when piped; it only pauses when started from Explorer.)
* **To do:** `--udp`, `--all`, `--pid`.

### `unpack`
* Lists and extracts archives through `7z.dll`.
* **Rough edges:** the `extract` verb has the help text of `list` ("List entry in archive."); progress percentage is a `todo`; `GetExtension` cuts at the *first* dot (`a.tar.gz` → `.tar.gz`, colors wrong for `my.file.txt`); unknown or missing archives now give a one-line error; no password option (encrypted archives fail), no "extract single file", no `create`.
* **To do:** fix help/extension, progress bar, `--password`, `--include` pattern.

### `download`
* HTTP download with proxy/timeout/header options.
* **Missing:** the `TODO Output to STDOUT instead File Download`; a malformed URL now gives a one-line error; no resume of interrupted downloads. Uses the obsolete `WebClient`.
* **To do:** stdout mode (`-O -`), `HttpClient` instead of `WebClient`, resume.

### `img`
* Renders an image with background-colored cells, scaled to the console. Uses `System.Drawing` (Windows only, GDI+).
* **Missing:** no usage text (a missing argument or file ends in a one-line error now);  no option for width/height or for half-block rendering (2× vertical resolution); no animated GIFs.
* **To do:** usage text, `--width/--height`, consider `ImageSharp` to become cross-platform.

### `bits`
* Converts numbers/bits. The source has `TODO SI niut prefixes`.
* **To do:** SI/IEC prefix handling, a clear message for empty input.

### `certy`
* Shows TLS certificate, chain and PEM of `host:port`.
* An unreachable host or a handshake error now prints one line and exits with code 1.
* **To do:** `--timeout`, `--sni`.

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
| `rip` | complete: every argument and wildcard is expanded once (the old loop moved the first file N times), each file is reported, the exit code is 1 if something was not found or could not be moved, `-q` for no output. |
| `free` | complete: CPU load is measured over 500 ms (it always showed 0 % before). |
| `rmline` | complete: `rmline file.ext:LINE [...]` deletes the line(s), keeps encoding/BOM and line endings, replaces the file atomically, exit code 1 on errors. Also accepts `~/` paths and several lines per file. |
| `consoleutils.update` | rewritten: GitHub API instead of a VERSION download, SHA-256 verification, zip-slip protection, files in use are renamed, rollback on errors, `check` with exit codes for scripts, `--yes`/`--force`. Tested against a local fake server (success, wrong checksum, zip slip, missing checksum, failure with rollback); not yet against a real release made by the new workflow. |

---

## Cross-cutting work (all tools)

1. ~~Redirected input/output~~ **done:** `ConsoleHelper.Width`, `Height`, `IsInteractive`, `TryClear()`, `TrySetCursorVisible()` in `cross.core` never throw; the tools use them. `ed` and `log` say that they need a console instead of crashing.
2. ~~"Press any key" from Explorer~~ **done:** `windows.core.ConsoleWindow.PauseIfStartedFromExplorer()` replaces the copies (only if a person can press a key).
3. ~~Stack traces for bad input~~ **done:** `ConsoleHelper.Run(...)` wraps `Main` in `bits`, `img`, `download`, `certy`, `unpack`, `hash`, `free` and `tulpn`: one readable line and exit code 1. `CONSOLEUTILS_DEBUG=1` shows the complete exception. Tools that handle errors themselves are not wrapped yet.
4. **Exit codes.** Better, but several tools still return 0 on errors (`seek`, `hash`).
5. **Tests.** Only `cross.core.tests` exists (10 tests for `AnsiText`). The pure logic (`subnet`, `bits`, `hash`, `chaos`, `CmdParser`, `FileAndDirectoryFilter`, `UnitHelper`) is easy to cover with xUnit.
6. **Tool-specific copies** of shared code: `kb` and `rip` (own `FileAndDirectoryFilter`), `events` (`EventColors` palette, own `Loader`).
7. **Measuring colored text.** Use `AnsiText` from `cross.core` (`VisibleLength`, `PadRightVisible`, `TruncateVisible`, `Strip`) instead of `string.Length`/`PadRight` on strings that went through `.Pastel()`. `list`, `unpack` and `tulpn` still pad before coloring and could use it.
