# Skill-To-Phase Mapping

## Purpose

Map OMX skills to the phase that owns the work.

## Mapping

| Skill | Phase | Use |
| --- | --- | --- |
| `deep-interview` | `Clarify` | intent, boundaries, non-goals, or decision ownership are unclear |
| `plan` | `Plan` | requirements need structured planning before execution |
| `ralplan` | `Plan` | requirements are clear enough for consensus planning but not execution yet |
| `ralph` | `Develop` and `Test` support | default single-owner execution loop |
| `team` | `Develop` and `Test` support | coordinated multi-lane execution |
| `autopilot` | `Develop` and `Test` | OMX runtime autonomous execution only |
| `ultrawork` | `Develop` and `Test` | parallel execution across agents |
| `ultraqa` | `Test` | OMX runtime persistent verification |
| `ecomode` | `Develop` and `Test` | cost-aware parallel execution |
| `tdd` | `Test` | test-first workflows |
| `build-fix` | `Develop` and `Test` | build, type, and compile repair |
| `code-review` | `Test` | comprehensive review after changes |
| `receiving-code-review` | `Test` | independent evaluation of review feedback, contract fit, and technical validity |
| `security-review` | `Test` | security audit for trust boundary changes |
| `web-clone` | `Develop` and `Test` | end-to-end extraction, generation, and verification |
| `cancel` | `Deliver` / `Stop` | end active workflow and clean up state |
| `note` | any | capture working memory |
| `trace` | any | inspect workflow history and state |
| `help` | any | OMX usage support |
| `doctor` | any | OMX installation or environment diagnosis |

## Rules

- the workflow determines the skill
- skills do not replace the workflow
- `Develop` must enter through exactly one owner: `ralph` or `team`
