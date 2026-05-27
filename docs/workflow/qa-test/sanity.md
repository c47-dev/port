# Sanity

## Purpose

Run local checks that match the changed boundary before required QA lanes and code review.

## Allowed Checks

- lint
- build
- type check
- narrow local checks
- markdown or path consistency checks for documentation-only changes

## Rules

- sanity is mandatory for every `Change`
- passing sanity does not replace API QA
- passing sanity does not replace browser UI QA
- passing sanity does not replace code review

## Required Output

1. sanity checks run
2. pass/fail result
3. failure evidence, if any
