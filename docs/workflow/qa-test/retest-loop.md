# Retest Loop

## Purpose

Fix and retest whenever sanity, any required QA lane, QA report completeness, or code review fails.

## Required Actions

1. collect the failure evidence
2. localize the failure to the owning boundary
3. fix the issue in the main agent flow
4. rerun sanity
5. rerun only the failed required QA lane
6. rerun `code-review`
7. rerun `receiving-code-review`

## Rules

- repeat until every mandatory test procedure passes
- stop only if:
  - all mandatory test procedures pass
  - a real blocker is identified
  - the user stops the task
- do not stop after the first QA failure
- do not report partial success while required QA is still failing

## Required Output

1. whether a QA loop was required
2. which boundary failed
3. whether both review lanes passed or are blocked
4. whether retest passed or is blocked
