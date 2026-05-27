# Dual Code Review

## Purpose

Run two independent review lanes after required QA evidence exists.

## Required Actions

- run `code-review`
- run `receiving-code-review`
- gather both review results before delivery
- classify each review lane as `pass`, `fail`, or `blocked`
- treat correctness, regression, contract drift, invalid review handling, and missing-test findings as blockers

## Rules

- `code-review` performs comprehensive correctness, regression, contract drift, and missing-test review
- `receiving-code-review` independently evaluates review feedback, contract fit, and technical validity
- both review lanes are mandatory for every `Change`
- review cannot be skipped because sanity or QA passed
- blocking findings must be fixed before delivery
- either lane returning `fail` blocks delivery

## Required Output

1. `code-review` result
2. `receiving-code-review` result
3. aggregated review verdict
4. blocking findings, if any
5. remaining risk, if any
