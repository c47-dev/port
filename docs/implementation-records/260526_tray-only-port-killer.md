# Implementation record: Tray-only PortKiller (Windows)

| Date | 2026-05-26 |
| Status | Shipped (build verified) |

## Summary

Forked `productdevbook/port-killer` Windows WPF project into `src/PortKiller/`. Delivered tray-only app with Liquid Glass popup for local TCP port list and process kill.

## Run

```powershell
cd src/PortKiller
dotnet run
# or
.\bin\Release\net8.0-windows\win-x64\publish\PortKiller.exe
```

## Architecture

- `TrayHost` — system tray lifecycle, toggle popup
- `TrayPopupWindow` — Liquid Glass UI (Acrylic + themed XAML)
- `TrayViewModel` — scan, filter, kill, auto-refresh
- `PortScannerService` / `ProcessKillerService` — unchanged from upstream

## Plan reference

`docs/plan/260526_tray-only-port-killer.md`
