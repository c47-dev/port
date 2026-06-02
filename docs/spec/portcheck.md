# PortCheck Product Contract

## Purpose

Windows tray utility to list local TCP listening ports and terminate owning processes. Optional **Docker Port** surface appears when Docker-related services are actively listening on local TCP ports. A persistent **Favourite Ports** surface lets the user pin host ports and keep them visible even when they are not currently listening. A `Settings` subpage inside the popup manages user-excluded host ports and the local refresh interval.

PortCheck never installs, starts, or wakes Docker Desktop or the Docker Engine.

## Actors

| Actor | Capability |
| --- | --- |
| User | View `Local Port` listeners; when Docker surface is visible, view `Docker Port` rows; view `Favourite Ports`; star/unstar host ports from Local or Docker rows; search; kill local PID; kill container-backed Docker row; kill all local PIDs; refresh; open Settings; add/remove excluded ports; change refresh interval; hide popup; quit |
| OS | Requires administrator elevation for reliable process termination in Release builds |
| Docker Engine | Used only through passive local control surfaces that are already available: named-pipe HTTP or an integrated WSL Docker CLI session |

## Surfaces

| Surface | Responsibility |
| --- | --- |
| `TrayHost` | Tray icon, left-click popup toggle, right-click menu (`Refresh`, `Kill All local`, `Quit`) |
| `TrayPopupWindow` - `Favourite Ports` | User-pinned host ports. Active rows reuse local host-listener details. Idle rows render `PortInfo.Inactive(hostPort)` and remain removable through the row star affordance |
| `TrayPopupWindow` - `Local Port` | Host TCP listeners; per-row kill by PID; `Kill All` inside this section |
| `TrayPopupWindow` - `Docker Port` | Docker-related listening ports. Prefer Engine catalog rows when available; otherwise show inferred rows from live docker-related local listeners. Per-row `Kill` always means `docker stop` and is only available when the row resolves to a Docker container identity |
| `TrayPopupWindow` - `Settings` | Add/remove user-excluded host ports and change refresh interval in seconds |
| `TrayViewModel` | Popup surface state, local/docker/favourite collections, exclusion gate, search, refresh orchestration, pane state, settings state, `IsDockerSurfaceVisible` gate |
| `PortScannerService` | Enumerate listening TCP ports via Win32 `GetExtendedTcpTable` |
| `ProcessKillerService` | Terminate process by PID (`Local Port`) |
| `DockerEngineClient` | Passive named-pipe HTTP to configured Docker API pipe; never start Docker |
| `DockerWslCliClient` | Passive WSL-integrated `docker` CLI adapter when a distro already has Docker Desktop integration |
| `DockerPortCatalogService` | Published TCP rows from the active Docker control surface |
| `DockerContainerStopService` | `docker stop` on the active Docker control surface |
| `ProtectedPortCatalogService` | Load built-in Windows protected host ports from `Config/protected-ports.json` |
| `SettingsService` | Load/save `%AppData%/PortCheck/settings.json` |
| `PortExclusionService` | Merge built-in protected ports with user-excluded ports and answer `IsExcluded(hostPort)` |
| `FavouritePortsService` | Load/save pinned host ports, prune excluded/protected ports, and build active/inactive favourite rows |
| `ConfirmDialog` | Kill-all and inline kill confirmation |

## UI Material Contract

Visual tokens, reusable modules, motion, and composition rules: [`liquid-glass-uiux.md`](liquid-glass-uiux.md).

- `Kill All`, `Refresh`, `Settings`, and `Hide` define the popup's action foundation.
- `Local Port` and `Docker Port` rows are not footer chips and must not reuse footer button architecture.
- In the idle state, port rows render as plain content on the shared popup bubble with no per-row glass card, no persistent row border, and no persistent translucent capsule.
- Per-row glass surface appears only on hover, selection, confirm, or explicit row action reveal.
- The popup shell remains the only glass-heavy container. `Settings` content stays inside the same shell and must not introduce nested heavy glass panels.

## Docker Surface Gate

`IsDockerSurfaceVisible` is `true` when either of the following holds:

1. `dockerCatalogEnabled` is true and Engine catalog fetch returns at least one published TCP mapping from a running container.
2. At least one live local listening port belongs to a docker-related process such as `com.docker.backend`, `docker-proxy`, `wslrelay`, or another process explicitly classified by the app as Docker-related.

When control-surface catalog rows exist, they are the preferred Docker pane rows.

When no container-resolved catalog rows exist but docker-related local listeners do exist, the popup still shows `Docker Port` using inferred rows derived from those live listeners.

Otherwise the popup shows `Local Port` only with no Docker segment and no Docker-missing copy.

If the user was on `Docker Port` and the gate becomes false, UI switches back to `Local Port` automatically.

## Exclusion Contract

- One global host-port exclusion rule applies to both `Local Port` and `Docker Port`.
- Built-in protected ports come from `Config/protected-ports.json` beside the app output. This file is a shipped contract source and invalid content is a startup failure.
- User-excluded ports come from `%AppData%/PortCheck/settings.json`.
- Effective excluded ports are the union of built-in protected ports and user-excluded ports.
- Excluded host ports are invisible everywhere in the popup:
  - no Favourite row
  - no Local row
  - no Docker row
  - no search hit
  - no `Kill All` target
  - no inline kill target
- Built-in protected ports are not shown in `Settings` and cannot be edited.
- If a stale row reaches a kill command after exclusion changes, the command must no-op.

## User Stories

1. Launch app; only tray icon is visible.
2. Left-click tray; popup opens on the `Local Port` surface.
3. `Local Port`: search by port, process name, or PID; non-excluded host listeners are shown; rows associated with Docker-related listeners show a Docker indicator.
4. `Local Port`: hover row; inline star toggles pinned state for that host port; inline kill confirm terminates that PID.
5. `Favourite Ports`: always-available pane tab; lists pinned host ports only. Listening ports reuse live local-row details. Non-listening pinned ports stay visible as `Not running`.
6. `Local Port`: `Kill All`; confirmation; terminate all non-excluded listed local PIDs. `Kill All` does not appear on `Favourite Ports` or `Docker Port`.
7. When Docker gate is true, switch to `Docker Port`; each non-excluded row shows host port and host address. Catalog-backed rows also show mapping to container port/protocol, container name, and compose labels.
8. `Docker Port`: hover row; inline star toggles pinned state for that host port; inline `Kill` confirm runs `docker stop` for rows resolved to a Docker container identity.
9. Footer action order is `Refresh`, `Settings`, `Hide`.
10. `Settings`: add/remove user-excluded host ports and change refresh interval in seconds. Settings does not add or remove favourites directly.
11. `Settings`: built-in protected ports remain hidden and are not editable.
12. Right-click tray; `Quit`; exit application.

## Keyboard Shortcuts

| Key | Action |
| --- | --- |
| `Ctrl+R` | Refresh |
| `Ctrl+K` | Kill All (`Local Port` active only; same as Local section `Kill All`) |
| `Esc` on `Settings` | Return to `Local Port` or the current ports surface |
| `Esc` on ports surfaces | Hide popup |

## Configuration

### `appsettings.json`

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
| `refreshIntervalSeconds` | 5 | Local Win32 scan interval default; user setting can override |
| `dockerEnginePipeName` | `docker_engine` | Preferred Docker API pipe name |
| `dockerCatalogEnabled` | true | When false, skip Engine catalog fetch; inferred docker listeners may still surface Docker pane |
| `dockerEngineTimeoutMs` | 2000 | Full catalog HTTP timeout |
| `dockerCliTimeoutMs` | 5000 | WSL-integrated Docker CLI timeout |
| `dockerCliWslDistribution` | empty | Explicit WSL distro name for Docker CLI; empty means probe non-`docker-desktop` distros for an integrated `docker` command |
| `dockerEngineProbeTimeoutMs` | 400 | Passive connect probe |
| `skipHeavyProcessInfoForDockerProxy` | true | Skip WMI command line for known Docker-related process names |

### `Config/protected-ports.json`

```json
{
  "ports": [7, 9, 13, 17, 19, 20, 21, 53, 67, 68, 88, 135, 137, 138, 139, 445, 464, 1900, 2869, 3389, 5353, 5355, 5357, 7680]
}
```

- Source of built-in Windows protected host ports
- Must ship with the app
- Invalid content is a startup failure

### `%AppData%/PortCheck/settings.json`

```json
{
  "refreshIntervalSeconds": 10,
  "userExcludedPorts": [3000, 5432],
  "favouritePorts": [8080, 15432]
}
```

| Field | Type | Required | Default | Notes |
| --- | --- | --- | --- | --- |
| `refreshIntervalSeconds` | `int` | no | from `appsettings.json` | Clamped to `3..20`; applied immediately after a valid edit |
| `userExcludedPorts` | `int[]` | no | `[]` | Unique ports in `1..65535` |
| `favouritePorts` | `int[]` | no | `[]` | Unique host ports in `1..65535`, sorted on save, max `32`, pruned when excluded/protected |

## Non-Goals

- Starting, installing, or enabling Docker Desktop / Engine
- Any UI copy when Docker is missing
- Remote or network-wide port scanning
- UDP or non-TCP protocols
- Stopped containers (`all=true`) in Docker list
- Persistent storage of port history
- HTTP API or web UI
- Podman / non-Docker engines
- Built-in protected-port editing in the UI

## Success Criteria

- `Local Port` lists current listening TCP ports with process name and PID, excluding effective excluded host ports.
- `Docker Port` appears when catalog-backed rows or inferred docker-related local listeners are present after exclusion is applied.
- Catalog-backed Docker rows show full port detail and support `docker stop`.
- Search filters only the active pane and never resurrects excluded rows.
- `Favourite Ports` always remains selectable and shows pinned active or inactive host ports only.
- Kill single and kill all respect confirmation UI and exclusion guards.
- Row star toggles persist favourite host ports and ignore excluded/protected ports.
- `Settings` persists user-excluded ports and refresh interval.
- Built-in protected ports never appear in rendered UI and cannot be killed from PortCheck.
- Tray app survives popup hide; only `Quit` exits.
- Debug build runs without UAC (`asInvoker`); Release publish requests elevation for kill.
- Refresh uses already-available local Docker control surfaces only and never starts Docker.

## Verification Notes

- Build: `dotnet build` in `src/PortCheck`
- Publish: `dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true`
- Kill tests require elevation in Release; document elevation in QA evidence
- Settings validation:
  - built-in protected ports never render
  - adding a user-excluded port hides matching Local and Docker host-port rows
  - adding a user-excluded port prunes the same host port from Favourite Ports
  - removing a user-excluded port allows the row to return on the next refresh
  - refresh interval change persists after restart
  - screenshot evidence includes the `Settings` surface
- Favourite Ports validation:
  - star on Local row adds the host port to `Favourite Ports`
  - star on Docker row adds the same host port to `Favourite Ports`
  - inactive favourite rows render `Not running` with no kill affordance
  - restart preserves favourites from `%AppData%/PortCheck/settings.json`
