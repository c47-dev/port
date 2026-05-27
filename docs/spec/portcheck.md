# PortCheck Product Contract

## Purpose

Windows tray utility to list local TCP listening ports and terminate owning processes. Optional **Docker Port** surface appears when Docker-related services are actively listening on local TCP ports. When Engine catalog data is available, Docker rows prefer container-backed published mappings. No main window; interaction is tray icon + popup only.

PortCheck never installs, starts, or wakes Docker Desktop or the Docker Engine.

## Actors

| Actor | Capability |
| --- | --- |
| User | View **Local Port** listeners; when Docker surface is visible, view **Docker Port** rows; search; kill local PID; kill (= `docker stop`) catalog-backed container row; kill all local PIDs (Local section only); refresh; hide popup; quit |
| OS | Requires administrator elevation for reliable process termination in Release builds |
| Docker Engine | Used only through passive local control surfaces that are already available: named-pipe HTTP or an integrated WSL Docker CLI session |

## Surfaces

| Surface | Responsibility |
| --- | --- |
| `TrayHost` | Tray icon, left-click popup toggle, right-click menu (Refresh / Kill All local / Quit) |
| `TrayPopupWindow` - **Local Port** | Host TCP listeners; per-row kill by PID; **Kill All** inside this section |
| `TrayPopupWindow` - **Docker Port** | Docker-related listening ports. Prefer Engine catalog rows when available; otherwise show inferred rows from live docker-related local listeners. Per-row **Kill** always means `docker stop` and is only available when the row is resolved to a Docker container identity |
| `TrayViewModel` | Dual collections, filter, refresh orchestration, pane state, `IsDockerSurfaceVisible` gate |
| `PortScannerService` | Enumerate listening TCP ports via Win32 `GetExtendedTcpTable` |
| `ProcessKillerService` | Terminate process by PID (Local pane) |
| `DockerEngineClient` | Passive named-pipe HTTP to configured Docker API pipe; never start Docker |
| `DockerWslCliClient` | Passive WSL-integrated `docker` CLI adapter when a distro already has Docker Desktop integration |
| `DockerPortCatalogService` | Published TCP rows from the active Docker control surface |
| `DockerContainerStopService` | `docker stop` on the active Docker control surface |
| `ConfirmDialog` | Kill-all and inline kill confirmation |

## UI Material Contract

- `Kill All`, `Refresh`, and `Hide` define the popup's canonical liquid glass action foundation.
- `Local Port` and `Docker Port` rows are not footer chips and must not reuse footer button architecture.
- In the idle state, port rows render as plain content on the shared popup bubble with no per-row glass card, no persistent row border, and no persistent translucent capsule.
- Per-row glass surface appears only on hover, selection, confirm, or explicit row action reveal.
- Inline row `Kill` and dismiss `X` controls must appear crisp, centered, slightly brighter than surrounding row text, and borderless in perception.
- Pane tabs and search bar are separate UI systems and are not part of this action-foundation contract.

## Docker Surface Gate

`IsDockerSurfaceVisible` is `true` when either of the following holds:

1. `dockerCatalogEnabled` is true and Engine catalog fetch returns at least one published TCP mapping from a running container.
2. At least one live local listening port belongs to a docker-related process such as `com.docker.backend`, `docker-proxy`, `wslrelay`, or another process explicitly classified by the app as Docker-related.

When control-surface catalog rows exist, they are the preferred Docker pane rows.

When no container-resolved catalog rows exist but docker-related local listeners do exist, the popup still shows **Docker Port** using inferred rows derived from those live listeners.

Otherwise the popup shows **Local Port only** with no Docker segment and no Docker-missing copy.

If the user was on Docker Port and the gate becomes false, UI switches back to Local Port automatically.

## User Stories

1. Launch app - only tray icon visible.
2. Left-click tray - toggle port list popup; default **Local Port** pane.
3. **Local Port:** search by port, process name, or PID; all host listeners shown (including docker-related processes); rows associated with Docker-related listeners show a Docker indicator.
4. **Local Port:** hover row - inline kill confirm - terminate that PID.
5. **Local Port:** **Kill All** (in Local section only) - confirmation - terminate all listed local PIDs.
6. When Docker gate is true, switch to **Docker Port** - each row shows host port and host address. Catalog-backed rows also show mapping to container port/protocol, container name, and compose labels. Inferred rows identify the docker-related local listener source.
7. **Docker Port:** hover row - **Kill** confirm - `docker stop` for rows resolved to a Docker container identity.
8. Footer **Refresh** / **Hide**; app remains in tray when hidden.
9. Right-click tray - **Quit** - exit application.

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
    "dockerCliTimeoutMs": 5000,
    "dockerCliWslDistribution": "",
    "dockerEngineProbeTimeoutMs": 400,
    "skipHeavyProcessInfoForDockerProxy": true
  }
}
```

| Key | Default | Notes |
| --- | --- | --- |
| `refreshIntervalSeconds` | 5 | Local Win32 scan interval |
| `dockerEnginePipeName` | `docker_engine` | Preferred Docker API pipe name |
| `dockerCatalogEnabled` | true | When false, skip Engine catalog fetch; inferred docker listeners may still surface Docker pane |
| `dockerEngineTimeoutMs` | 2000 | Full catalog HTTP timeout |
| `dockerCliTimeoutMs` | 5000 | WSL-integrated Docker CLI timeout |
| `dockerCliWslDistribution` | empty | Explicit WSL distro name for Docker CLI; empty means probe non-`docker-desktop` distros for an integrated `docker` command |
| `dockerEngineProbeTimeoutMs` | 400 | Passive connect probe |
| `skipHeavyProcessInfoForDockerProxy` | true | Skip WMI command line for known Docker-related process names |

## Non-Goals

- Starting, installing, or enabling Docker Desktop / Engine
- Any UI copy when Docker is missing
- Remote or network-wide port scanning
- UDP or non-TCP protocols
- Stopped containers (`all=true`) in Docker list
- Persistent storage of port history
- HTTP API or web UI
- Podman / non-Docker engines (v1)

## Success Criteria

- Local Port lists current listening TCP ports with process name and PID.
- Docker Port segment appears when catalog-backed rows or inferred docker-related local listeners are present.
- Catalog-backed Docker rows show full port detail and support `docker stop`.
- Inferred Docker rows expose the live docker-related listening port even when Engine catalog data is unavailable.
- Search filters the active pane without rescanning on every keystroke.
- Kill single and kill all respect confirmation UI.
- Tray app survives popup hide; only **Quit** exits.
- Debug build runs without UAC (`asInvoker`); Release publish requests elevation for kill.
- Refresh uses already-available local Docker control surfaces only and never starts Docker.

## Verification Notes

- Build: `dotnet build` in `src/PortCheck`
- Publish: `dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true`
- Kill tests require elevation in Release; document elevation in QA evidence
- Docker tests:
  - docker-related local listener present -> Docker segment visible
  - Engine catalog row present -> Docker pane shows catalog-backed row
  - no docker-related local listener and no catalog row -> no Docker segment
