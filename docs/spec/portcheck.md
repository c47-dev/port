# PortCheck — Product Contract

## Purpose

Windows tray utility to list local TCP listening ports and terminate owning processes. No main window; interaction is tray icon + popup only.

## Actors

| Actor | Capability |
| --- | --- |
| User | View ports, search, kill one process, kill all, refresh, hide popup, quit app |
| OS | Requires administrator elevation for reliable process termination in Release builds |

## Surfaces

| Surface | Responsibility |
| --- | --- |
| `TrayHost` | Tray icon, left-click popup toggle, right-click menu (Refresh / Kill All / Quit) |
| `TrayPopupWindow` | Liquid Glass popup: search, port list, footer actions |
| `TrayViewModel` | Ports, filter, refresh timer, kill commands |
| `PortScannerService` | Enumerate listening TCP ports via Win32 `GetExtendedTcpTable` |
| `ProcessKillerService` | Terminate process by PID |
| `ConfirmDialog` | Kill-all and inline kill confirmation |

## User Stories

1. Launch app → only tray icon visible.
2. Left-click tray → toggle port list popup.
3. Search by port number, process name, or PID.
4. Hover row → inline kill confirm → terminate that PID.
5. Footer **Kill All** → confirmation dialog → terminate all listed PIDs.
6. Footer **Hide** → close popup; app remains in tray.
7. Right-click tray → **Quit** → exit application.

## Keyboard Shortcuts

| Key | Action |
| --- | --- |
| Ctrl+R | Refresh port list |
| Ctrl+K | Kill All (with confirmation) |
| Esc | Hide popup |

## Configuration

`appsettings.json` beside the executable:

```json
{
  "appSettings": {
    "refreshIntervalSeconds": 5
  }
}
```

Default refresh interval: 5 seconds. View model reads `RefreshIntervalSeconds` at startup.

## Non-Goals

- Remote or network-wide port scanning
- UDP or non-TCP protocols
- Persistent storage of port history
- HTTP API or web UI

## Success Criteria

- Popup lists current listening TCP ports with process name and PID.
- Search filters the visible list without rescanning on every keystroke (filter in view model).
- Kill single and kill all respect confirmation UI.
- Tray app survives popup hide; only **Quit** exits.
- Debug build runs without UAC (`asInvoker`); Release publish requests elevation for kill.

## Verification Notes

- Build: `dotnet build` in `src/PortCheck`
- Publish: `dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true`
- Kill tests require elevation in Release; document elevation in QA evidence
