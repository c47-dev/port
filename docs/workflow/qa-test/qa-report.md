# QA Report

## Purpose

Merge returned QA evidence into one report.

## Required Content

- which lanes ran
- which checks ran
- what evidence was collected
- pass, fail, or blocked per lane
- tooling limits, if any

## Rules

- QA report is mandatory whenever any QA lane ran
- missing evidence is a test failure, not a formatting omission
- the main agent owns delivery decisions; QA lanes provide evidence

## Required Output

1. lanes run
2. checks executed
3. evidence collected
4. lane verdicts
5. report status: `pass` / `fail` / `blocked`
