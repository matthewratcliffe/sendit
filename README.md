# SendIt

AI-assisted Git workflow automation: standardises commits, enforces branch/ticket
conventions, runs your tests, and commits/pushes — all from a single `sendit` command.
See [SPEC.md](SPEC.md) for the full functional specification.

## Install

### Windows (PowerShell)

```powershell
irm https://raw.githubusercontent.com/matthewratcliffe/sendit/main/packaging/install.ps1 | iex
```

This downloads the latest self-contained `sendit.exe` release, installs it to
`%LOCALAPPDATA%\Programs\SendIt`, and adds that folder to your user `PATH`.
Open a new terminal afterwards so the updated `PATH` takes effect.

### Linux / macOS

```bash
curl -fsSL https://raw.githubusercontent.com/matthewratcliffe/sendit/main/packaging/install.sh | bash
```

Installs to `~/.local/bin` by default (override with `SENDIT_INSTALL_DIR`). Add that
directory to your `PATH` if the script tells you it isn't already there.

### From source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/matthewratcliffe/sendit.git
cd sendit
dotnet build
dotnet run --project src/SendIt.Cli -- --help
```

## Usage

```
sendit                Run the complete workflow
sendit -configure      Launch the Terminal.Gui configuration screen
sendit -doctor         Run diagnostics
sendit -reset          Delete user configuration
sendit -skiptests      Skip project validation
sendit -force          Override warnings
sendit --version       Display version
sendit --help          Display usage
```

## Configuration

Two JSON files, merged at load time (repository overrides user):

- `%USERPROFILE%\.sendit.json` (or `$HOME/.sendit.json`) — global defaults, may include
  an AI API key (encrypted at rest with Windows DPAPI when running on Windows).
- `<repo>\.sendit.json` — repository-specific overrides, committed to source control.
  SendIt never writes an API key into this file.

Run `sendit -configure` for an interactive, keyboard-navigable editor covering General,
AI, Git, Tests, and Advanced settings.

## AI providers

Configurable via `sendit -configure`: OpenAI-compatible endpoints, Ollama, LM Studio,
llama.cpp, or a local CLI tool (Claude Code, Codex CLI, Kiro CLI, OpenCode, or any
custom command) invoked by piping the prompt over stdin. If no provider is reachable,
SendIt falls back to prompting you for a commit message directly.

## Building release artifacts

```powershell
./packaging/publish.ps1
```

Produces self-contained single-file binaries for win-x64, win-arm64, linux-x64,
linux-arm64, osx-x64, and osx-arm64 under `./artifacts`, packaged as `.zip` (Windows)
or `.tar.gz` (Linux/macOS) for GitHub Releases. Pushing a `v*` tag triggers
`.github/workflows/release.yml` to do this automatically.

## Tests

```bash
dotnet test
```
