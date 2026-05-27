# Browser UI QA

Use this document only for the Browser UI QA subagent.

Browser UI QA must use `agent-browser`.

## Required Inputs

- target system
- governing spec path
- affected browser-visible surfaces
- browser UI QA checklist

## Login Credential

When authentication is required, use the repository-specific QA credential configured in:

- root `CLAUDE.md`

## Execution Rules

- this lane is mandatory whenever browser UI QA is classified as required
- use `agent-browser`
- execute the real browser flow
- when login is required, use the configured QA username and password from `CLAUDE.md`
- use an isolated named incognito browser session
- keep network monitoring on the same browser session as the UI flow
- read `docs/workflow/qa-test/browser-network-monitor.md` when the verified flow issues requests that affect auth, persistence, visible state, permissions, or sensitive data
- for request-bearing flows, collect network evidence in parallel with UI verification
- do not replace browser QA with source-code-only review
- verify every checklist item
- collect concrete evidence
- report tooling limits explicitly when request or response bodies are not retrievable
- this output becomes the browser UI QA section of the QA report

## Required Checks

For every checklist item, verify:

- required elements are present
- interaction flow works
- visible state changes are correct
- visible error rendering is correct

For every relevant network request, verify when retrievable:

- URL, method, and status are expected
- request body does not contain unexpected or sensitive data
- response body matches the visible UI state
- latency evidence is recorded
- duplicate requests, unexpected retries, and session contamination are absent

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

## Failure Rule

If any required check fails:

- return `fail`
- point to the failing surface
- include the concrete evidence

Do not propose code changes.
