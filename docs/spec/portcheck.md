# PortCheck — Product Contract

## Purpose

Windows tray utility to list local TCP listening ports and terminate owning processes. Optional **Docker Port** surface appears only when Docker Engine is already running locally and at least one running container publishes a TCP port to the host. No main window; interaction is tray icon + popup only.

PortCheck **never** installs, starts, or wakes Docker Desktop or the Docker Engine.

## Actors

| Actor | Capability |
| --- | --- |
| User | View **Local Port** listeners; when Docker surface is visible, view **Docker Port** mappings; search; kill local PID; kill (= docker stop) container; kill all local PIDs (Local section only); refresh; hide popup; quit |
| OS | Requires administrator elevation for reliable process termination in Release builds |
| Docker Engine | Used only via passive connection to `//./pipe/docker_engine` when already available |

## Surfaces

| Surface | Responsibility |
| --- | --- |
| `TrayHost` | Tray icon, left-click popup toggle, right-click menu (Refresh / Kill All local / Quit) |
| `TrayPopupWindow` — **Local Port** | Host TCP listeners; per-row kill by PID; **Kill All** inside this section |
| `TrayPopupWindow` — **Docker Port** | Running containers’ published TCP mappings (visible only when Engine reachable **and** ≥1 published TCP); per-row **Kill** (= `docker stop`) |
| `TrayViewModel` | Dual collections, filter, refresh orchestration, pane state, `IsDockerSurfaceVisible` gate |
| `PortScannerService` | Enumerate listening TCP ports via Win32 `GetExtendedTcpTable` |
| `ProcessKillerService` | Terminate process by PID (Local pane) |
| `DockerEngineClient` | Passive named-pipe HTTP to Engine; never start Docker |
| `DockerPortCatalogService` | `GET /containers/json?all=false` → published TCP rows |
| `DockerContainerStopService` | `POST /containers/{id}/stop` (Docker pane Kill) |
| `ConfirmDialog` | Kill-all and inline kill confirmation |

## Docker Surface Gate

`IsDockerSurfaceVisible` is `true` only when **all** of the following hold:

1. `dockerCatalogEnabled` is true in config.
2. Passive pipe probe to the Engine succeeds (Engine already running).
3. Catalog fetch returns **at least one** published TCP mapping from a **running** container.

Otherwise the popup shows **Local Port only** — no Docker segment, no empty Docker state, and **no** messages such as “Docker unavailable” or “start Docker Desktop”.

If the user was on Docker Port and the gate becomes false, UI switches back to Local Port automatically.

## User Stories

1. Launch app → only tray icon visible.
2. Left-click tray → toggle port list popup; default **Local Port** pane.
3. **Local Port:** search by port, process name, or PID; all host listeners shown (including docker-proxy processes); rows mapped to a Docker publish show a **Docker** badge.
4. **Local Port:** hover row → inline kill confirm → terminate that PID.
5. **Local Port:** **Kill All** (in Local section only) → confirmation → terminate all listed local PIDs.
6. When Docker gate is true, switch to **Docker Port** → each row shows host port, host address, mapping to container port/protocol, container name, compose labels when present, and listening/idle badge.
7. **Docker Port:** hover row → **Kill** confirm → `docker stop` for that container.
8. Footer **Refresh** / **Hide**; app remains in tray when hidden.
9. Right-click tray → **Quit** → exit application.

## Keyboard Shortcuts

| Key | Action |
| --- | --- |
| Ctrl+R | Refresh |
| Ctrl+K | Kill All (Local pane active only; same as Local section Kill All) |
| Esc | Hide popup |

## Configuration

`appsettings.json` beside the executable:

```json
{
  "appSettings": {
    "refreshIntervalSeconds": 5,
    "dockerRefreshIntervalSeconds": 10,
    "dockerEnginePipeName": "docker_engine",
    "dockerCatalogEnabled": true,
    "dockerEngineTimeoutMs": 2000,
    "dockerEngineProbeTimeoutMs": 400,
    "skipHeavyProcessInfoForDockerProxy": true
  }
}
```

| Key | Default | Notes |
| --- | --- | --- |
| `refreshIntervalSeconds` | 5 | Local Win32 scan interval |
| `dockerEnginePipeName` | `docker_engine` | Windows pipe name |
| `dockerCatalogEnabled` | true | When false, never show Docker segment |
| `dockerEngineTimeoutMs` | 2000 | Full catalog HTTP timeout |
| `dockerEngineProbeTimeoutMs` | 400 | Passive connect probe |
| `skipHeavyProcessInfoForDockerProxy` | true | Skip WMI command line for known Docker proxy process names |

## Non-Goals

- Starting, installing, or enabling Docker Desktop / Engine
- Any UI copy when Docker is missing or has no published TCP ports
- Remote or network-wide port scanning
- UDP or non-TCP protocols
- Stopped containers (`all=true`) in Docker list
- Persistent storage of port history
- HTTP API or web UI
- Podman / non-Docker engines (v1)

## Success Criteria

- Local Port lists current listening TCP ports with process name and PID.
- Docker Port segment appears only when gate conditions are met.
- Docker rows show full port detail; **Kill** stops the container via Engine API.
- Search filters the active pane without rescanning on every keystroke.
- Kill single and kill all respect confirmation UI.
- Tray app survives popup hide; only **Quit** exits.
- Debug build runs without UAC (`asInvoker`); Release publish requests elevation for kill.
- Refresh uses named pipe only (no `docker.exe` subprocess).

## Verification Notes

- Build: `dotnet build` in `src/PortCheck`
- Publish: `dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true`
- Kill tests require elevation in Release; document elevation in QA evidence
- Docker tests: Engine running with `docker run -p`; Engine off → no Docker UI; Engine on with no publishes → no Docker segment
