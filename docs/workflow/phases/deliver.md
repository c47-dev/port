# Deliver

## Purpose

Use this phase only when all required verification has passed.

## Required Actions

- verify that the requested scope is finished
- verify that no required QA phase is still unresolved
- verify that the current task is ready to exit
- return only after fresh passing evidence exists

For a `Large` change:

- verify that every required phase is complete and verified
- verify that the global planning/implementation document reflects the final phase statuses and aggregate changelog
- do not deliver after only a partial phase subset unless the user explicitly scoped delivery to that subset

## Required Final Output

1. conclusion
2. actual changes
3. reasoning
4. verification results

## Blocking Rules

- do not deliver partial work as final
- do not stop after the first QA failure
- if verification fails, fix the cause and rerun the failed checks until pass or a real blocker exists
- for a `Large` change, do not treat one passing phase as final initiative delivery
