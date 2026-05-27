# Browser QA Network Monitor Workflow

Use this document only inside the Browser UI QA lane when the verified browser flow issues requests that affect correctness, data safety, auth state, or persistence behavior.

## Source Of Truth

- repository contract: `AGENTS.md`
- test entrypoint: `docs/workflow/test.md`
- browser lane execution document: `docs/workflow/qa-test/browser-ui-qa.md`
- browser automation capability: `C:/Users/Alex_Chiu/.agents/skills/agent-browser/SKILL.md`

## Architecture

| Layer | Responsibility |
| --- | --- |
| Browser UI QA agent | execute the real browser flow with `agent-browser` |
| Browser session | remain isolated with a named incognito session for that QA run |
| DevTools / CDP monitor | attach to the same Chromium session and inspect network evidence |
| QA report | combine visible UI evidence and network evidence into one verdict |

## Session Contract

- every browser QA run must use an explicit named incognito browser session
- browser UI QA must use the repository-specific QA credential defined in root `CLAUDE.md`
- the same session must be used for:
  - page navigation
  - interaction flow
  - network request inspection
  - latency evidence collection
- shared default sessions are invalid for required QA because they can hide session contamination, cached-cookie reuse, and request leakage

## Network Monitoring Contract

- browser QA must monitor network traffic in parallel with the UI flow
- the monitor must inspect navigational requests and API or data requests that can affect:
  - authentication
  - persistence
  - visible state
  - permission behavior
  - sensitive data exposure
- static asset requests are not required evidence unless they directly explain a visible failure

## Required Evidence

For each relevant request, collect when retrievable:

- URL
- HTTP method
- status code
- request headers relevant to auth or content type
- request body excerpt
- response headers relevant to auth, cache, or content type
- response body excerpt
- latency evidence in milliseconds

If a field is not retrievable from the active tooling, the report must say so explicitly instead of inventing data.

## Required Concern Checks

The QA report must explicitly check and classify:

- unexpected duplicate requests
- unexpected retries
- inconsistent response body versus visible UI state
- auth or cookie leakage across runs
- cross-session contamination
- unexpected error payloads hidden by UI fallback
- sensitive data exposure in request or response bodies

## Browser QA Verdict Rule

- browser QA passes only if both conditions are true:
  - visible UI checks pass
  - network concern checks pass
- any unresolved network concern affecting correctness or data safety returns `fail`
- tooling limits that block required evidence return `blocked`

## Execution Guidance

- use `agent-browser` for the browser flow
- when authentication is required, log in with the repository-specific QA credential from `CLAUDE.md`
- use `agent-browser` named incognito sessions to isolate concurrent QA runs
- use `agent-browser` network inspection and CDP attachment only when both operate on the same browser session
- use response-body evidence only where the active toolchain can retrieve it from that session

## Required Output

Return exactly:

1. surfaces tested
2. checks executed
3. network evidence collected
4. pass items
5. fail items
6. concerns found
7. tooling limits, if any
8. overall verdict: `pass` / `fail` / `blocked`
