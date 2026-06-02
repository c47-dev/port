# Sanity

## Purpose

Run local checks that match the changed boundary before required QA lanes and code review.

## Allowed Checks

- lint
- build
- type check
- narrow local checks
- markdown or path consistency checks for documentation-only changes
- PortCheck headless harness when `GlassRoundButton` or its template/animator changes:
  ```powershell
  Get-Process PortCheck -ErrorAction SilentlyContinue | Stop-Process -Force
  dotnet build src/PortCheck/PortCheck.csproj
  dotnet exec src/PortCheck/bin/Debug/net8.0-windows/PortCheck.dll --validate-glass-round-button
  Get-Content $env:TEMP\portcheck-glass-round-validate\glass-round-button-report.txt
  ```
  Require `PASS` in the report file, not only exit code `0`.

## Rules

- sanity is mandatory for every `Change`
- passing sanity does not replace API QA
- passing sanity does not replace browser UI QA
- passing sanity does not replace code review

## Required Output

1. sanity checks run
2. pass/fail result
3. failure evidence, if any
