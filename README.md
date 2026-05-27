# PortCheck

Windows tray app to **list local TCP listening ports** and **kill processes**. Runs in the system tray with a Liquid Glass popup — no main window.

## Requirements

- Windows 10 1809+ or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (build) or .NET 8 Runtime (framework-dependent publish)
- **Administrator** for Release builds (manifest requests elevation to kill processes)

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
    Popup --> BackdropBlurHelper
```

| Component | Role |
|-----------|------|
| `TrayHost` | Tray icon, popup toggle, tray menu (Refresh / Kill All / Quit) |
| `TrayPopupWindow` | Liquid Glass UI, search, port list, footer actions |
| `TrayViewModel` | Ports, filter, refresh timer, kill commands |
| `PortScannerService` | Win32 `GetExtendedTcpTable` |
| `ProcessKillerService` | Terminate by PID |

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

**Output:** `bin/Release/net8.0-windows/win-x64/publish/PortCheck.exe`

The executable uses `Assets/AppIcon.ico` (tray + file icon). `appsettings.json` is copied next to the exe.

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

## License

MIT — see [LICENSE](LICENSE).
