# Delegation Rules

- explorer agents locate systems, specs, and implementation boundaries
- worker agents handle isolated code changes with disjoint write scopes
- QA agents handle API QA and browser UI QA
- do not delegate unresolved ambiguity
- do not let multiple agents edit the same spec or boundary files

## Explorer Agents

Use explorer agents only for:

- locating systems
- locating specs
- locating implementation boundaries

## Worker Agents

Use worker agents only for:

- isolated code changes with disjoint write scopes

## QA Agents

Use QA agents only for:

- API QA
- browser UI QA

## Leader Responsibilities

- keep the user-facing brief current
- assign bounded work with explicit ownership
- integrate results
- own final verification

## Worker Responsibilities

- stay inside assigned scope
- do not widen scope silently
- report blockers, shared-file conflicts, or missing authority upward

## Rules

- required QA must use QA subagents
- do not delegate unresolved ambiguity
- do not let multiple agents edit the same spec or boundary files
