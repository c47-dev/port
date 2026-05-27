# QA Checklist Refresh

Use this document only for executable QA checklist refresh.

## Purpose

- turn the governing `docs/spec/*.md` contract plus the changed implementation boundaries into two executable checklists:
  - API QA checklist
  - browser UI QA checklist

## Read Inputs

Read only these inputs:

- governing `docs/spec/*.md`
- changed route files
- changed client or UI files
- changed persistence or library boundary files

## Build The API QA Checklist

Create one checklist item for each affected API behavior:

- request fields
- response fields
- status code
- error payload
- auth or permission rule
- persistence or side effect

Use this item format:

- surface:
- scenario:
- expected result:
- evidence required:
- scope: `new` / `changed` / `protected`

## Build The Browser UI QA Checklist

Create one checklist item for each affected browser-visible behavior:

- required element
- visible state change
- interaction flow
- visible error rendering

Use this item format:

- surface:
- scenario:
- expected result:
- evidence required:
- scope: `new` / `changed` / `protected`

## Output Contract

Return two outputs only:

1. API QA checklist
2. browser UI QA checklist

Rules:

- do not execute QA from this document
- do not decide delivery from this document
- do not preload lane-specific execution rules here
