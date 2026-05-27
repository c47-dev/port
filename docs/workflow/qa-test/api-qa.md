# API QA

Use this document only for the API QA subagent.

## Required Inputs

- target system
- governing spec path
- affected API surfaces
- API QA checklist

## Execution Rules

- this lane is mandatory whenever API QA is classified as required
- execute API QA against the real API surface
- do not replace API QA with source-only review
- verify every checklist item
- collect direct evidence for each checked surface
- report exact failures and exact blocked conditions
- this output becomes the API QA section of the QA report

## Required Checks

For every checklist item, verify:

- request shape
- response shape
- status code
- error payload
- side effect, if applicable

## Required Output

Return exactly:

1. surfaces tested
2. checks executed
3. pass items
4. fail items
5. evidence for each failure
6. overall verdict: `pass` / `fail` / `blocked`

## Failure Rule

If any required check fails:

- return `fail`
- point to the failing surface
- include the concrete evidence

Do not propose code changes.
