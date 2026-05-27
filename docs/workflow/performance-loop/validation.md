# Validation

## Purpose

Validate workflow patches before publishing them as active authority.

## Required Checks

- no active authority path becomes broken
- no mandatory QA rule is weakened
- no delivery rule is weakened
- no ambiguity-stop rule is weakened
- changed instructions are not duplicated across multiple authority files
- changed instructions are testable by repository search or contract review

## Required Output

1. patch under validation
2. checks executed
3. pass/fail result
4. blocker, if any
