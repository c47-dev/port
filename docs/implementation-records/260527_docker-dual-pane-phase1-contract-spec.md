# Phase 1: Contract & Spec — Docker Dual Pane

| Field | Value |
| --- | --- |
| Initiative | [260527_docker-dual-pane-ports.md](260527_docker-dual-pane-ports.md) |
| Phase | 1 of 3 |
| Status | `complete` |
| Depends on | — |
| Governing authority | `docs/spec/portcheck.md`, `docs/workflow/phases/plan.md` |

---

## 1. Phase PRD

### Problem

Product contract 未描述 Local / Docker 雙列表、docker stop、與性能相關設定，實作無法通過 harness gate。

### Goals

- 更新 `docs/spec/portcheck.md` 成為本 initiative 的 canonical 契約。
- 凍結 segment 命名、Docker surface gate、Kill 語意（Docker=stop）、Kill All 位置、config keys。

### Non-Goals

- 程式碼變更（本 phase 僅 spec）。

### User Stories

1. 開發者僅讀 spec 即可實作雙 pane 與 Engine API 整合。
2. QA 可依 spec 撰寫 desktop 檢查清單。

### Success Criteria

- [ ] `docs/spec/portcheck.md` 包含 Surfaces、User Stories、Configuration、Non-Goals 更新。
- [ ] 與 global doc §7–11 無矛盾。
- [ ] Open Questions OQ-1、OQ-2 在 spec 中寫死預設決策。

### Scope Boundaries

僅 `docs/spec/portcheck.md`；README 可選一句「Docker pane requires Docker Desktop」。

---

## 2. Phase TDD

### Technical Approach

將 global initiative 的 UI / API / config 契約 **逐條寫入** spec，採產品語言 + 技術約束表。

### Component Breakdown

| Artifact | Action |
| --- | --- |
| `docs/spec/portcheck.md` | Add `Local Port` / `Docker Port` surfaces, keyboard table, appsettings |

### Ownership

| Owner | Responsibility |
| --- | --- |
| Leader / spec editor | Merge spec PR |
| Executor | 不進入 Develop 直到 Phase 1 exit |

### Failure Modes

| Failure | Handling |
| --- | --- |
| Spec 漏寫 Kill 語意 / surface gate | Phase 1 review checklist 對照 global §10.1、§10.7 |

### Validation Strategy

人工 diff review：spec 章節 checklist 100% 覆蓋 global §9–11。

### Test Strategy

無自動化；spec review sign-off。

---

## 3. Spec Delta Checklist (must appear in `portcheck.md`)

### Surfaces (add rows)

| Surface | Responsibility |
| --- | --- |
| `TrayPopupWindow` — Local Port pane | Host TCP listeners; kill by PID |
| `TrayPopupWindow` — Docker Port pane | Engine published TCP (**visible only when probe succeeds**); Kill = container stop |
| `DockerEngineClient` | Named-pipe HTTP; passive probe; **never start Docker** |
| `DockerPortCatalogService` | Parse containers → `DockerPortInfo` |
| `DockerContainerStopService` | `POST /containers/{id}/stop` |

### User Stories (add)

8. Switch **Local Port** / **Docker Port** segments.
9. Docker pane（**僅 Engine 已運行且 probe 成功時顯示**）lists **running** published TCP with **full port detail** (host port, host address, container port, protocol, container, compose).
10. **Kill** on Docker pane (= `docker stop`) with confirmation matching Local.
11. Docker not installed / Engine off / pipe fail → **no Docker UI at all** (no segment, no messages); PortCheck **never** starts Docker.
12. **Kill All** inside **Local Port** section only; not in global footer.
13. Local shows all listeners including docker-proxy; **Docker** badge when port is a published mapping.

### Configuration (add keys)

同 global §11。

### Non-Goals (add)

Podman、remote docker、UDP、stopped-container catalog (v1)、啟動/安裝 Docker、Docker 不可用時的任何 UI 提示。

### Success Criteria (add)

- Docker segment visible only when Engine already running (probe).
- When Docker unavailable: single Local surface, zero Docker copy.
- Kill All inside Local section only.
- No `docker.exe` spawn on refresh (pipe only).
- Compact row heights per UI contract.

---

## 4. Validation Plan

| Step | Action | Pass |
| --- | --- | --- |
| 1 | 對照 global doc §7–13 | All covered in spec |
| 2 | 對照 Open Questions | Defaults recorded in spec |
| 3 | Peer review | No blocking comments |

---

## 5. Test and Verify Contract

| Item | Required |
| --- | --- |
| `dotnet build` | Not required in Phase 1 |
| Spec review | **Required** |
| Phase 2 entry | Spec merged on branch |

---

## 6. Phase Changelog

| Date | Change |
| --- | --- |
| 2026-05-27 | Phase 1 document created |

---

## 7. Completion Criteria

- [ ] `docs/spec/portcheck.md` updated on branch.
- [ ] Global initiative doc §16 defaults reflected in spec.
- [ ] Phase status in global doc → `phase-1-complete`.

---

*Phase 1 — no code edits.*
