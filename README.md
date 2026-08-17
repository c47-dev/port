# PortCheck

[![PortCheck hero demo](docs/assets/portcheck-hero.gif)](docs/assets/portcheck-hero.mp4)

PortCheck is a focused Windows tray app for seeing every listening TCP port in one liquid-glass command surface. Search by port, process, or PID; favourite important ports; inspect local and Docker mappings; and stop the process that owns a port without opening Task Manager or Docker Desktop. Docker integration is read-only until you explicitly choose **Kill** (`docker stop` for containers), and PortCheck never starts or installs Docker.

[Download the PortCheck hero demo (MP4)](docs/assets/portcheck-hero.mp4)

## Requirements

- Windows 10 1809+ or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (build) or .NET 8 Runtime (framework-dependent publish)
- **Administrator** for Release builds (manifest requests elevation to kill processes)
- **Docker Desktop** (optional): only used if Engine is already running; no Docker UI when Engine is off or has no published TCP ports

## Architecture

```mermaid
flowchart TB
    App["App + DI"] --> TrayHost
    App --> TrayViewModel
    App --> TrayPopupWindow
    TrayHost --> TaskbarIcon
    TaskbarIcon -->|Left click| Popup["TrayPopupWindow"]
    TrayViewModel --> PortScannerService
    TrayViewModel --> ProcessKillerService
    TrayViewModel --> DockerPortCatalogService
    Popup --> BackdropBlurHelper
```

| Component | Role |
|-----------|------|
| `TrayHost` | Tray icon, popup toggle, tray menu (Refresh / Kill All / Quit) |
| `TrayPopupWindow` | Local Port / Docker Port panes, search, compact lists |
| `TrayViewModel` | Dual collections, filter, refresh, kill / docker stop |
| `PortScannerService` | Win32 `GetExtendedTcpTable` |
| `ProcessKillerService` | Terminate by PID (Local pane) |
| `DockerPortCatalogService` | Engine API via named pipe (no `docker.exe`) |
| `DockerContainerStopService` | Stop container (Docker pane Kill) |

Source: `src/PortCheck/`

## Build & run

```powershell
cd src/PortCheck
dotnet restore
dotnet build
dotnet run
```

Debug uses `asInvoker` (no UAC). Port scan works; killing may need an elevated shell or a Release publish.

## Publish (Windows `.exe`)

```powershell
cd src/PortCheck
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true
```

**Output folder:** `bin/Release/net8.0-windows/win-x64/publish/`

### Run the published app

1. Use the **entire `publish` folder** (do not copy only `PortCheck.exe`).
2. Double-click `PortCheck.exe` (or run from that folder in PowerShell).
3. Approve the **UAC elevation** prompt — Release builds use `requireAdministrator` in `app.manifest` so process kill works.

```powershell
cd src\PortCheck\bin\Release\net8.0-windows\win-x64\publish
.\PortCheck.exe
```

| Path (beside `PortCheck.exe`) | Required |
|------|----------|
| `PortCheck.exe` | Yes |
| `appsettings.json` | Yes (not embedded in single-file) |
| `Config/protected-ports.json` | Yes — app exits if missing |
| `Assets/AppIcon.ico` | Yes (tray icon; falls back to exe icon if missing) |

### Publish troubleshooting

| Symptom | Cause | Fix |
|--------|--------|-----|
| PowerShell `&&` error | Invalid in Windows PowerShell 5.x | Use `;` between commands, or run commands on separate lines |
| “Access denied” / exe won’t start | Release requires elevation | Approve UAC, or right-click **Run as administrator** |
| Message about `protected-ports.json` | Only the `.exe` was copied | Distribute the full `publish` folder including `Config\` |
| Tray appears then error on popup | Rare GPU/shader issue | Motion falls back without lens shader; rebuild from latest source |

Distribute the **whole `publish` folder**, not only `PortCheck.exe`.

## Usage

1. Launch — only the tray icon appears.
2. **Left-click** tray — toggle port list popup.
3. **Search** by port, process name, or PID.
4. **Hover** a row → **✕** → inline kill confirm.
5. Footer **Kill All** — confirmation dialog.
6. Footer **Hide** — closes popup; app stays in tray.
7. **Right-click** tray → **Quit** — exit app.

| Key | Action |
|-----|--------|
| Ctrl+R | Refresh |
| Ctrl+K | Kill All |
| Esc | Hide |

## Configuration

`appsettings.json`:

```json
{
  "appSettings": {
    "refreshIntervalSeconds": 5
  }
}
```
