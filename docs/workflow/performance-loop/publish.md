# Publish

## Purpose

Publish validated workflow patches into the correct authority file.

## Rules

- publish only validated patches
- update the narrowest authority file that owns the rule
- keep redirect files redirect-only
- keep historical plan files unchanged unless they are the requested target
- record the changed authority path in the final verification results

## Required Output

1. published patch id
2. changed authority files
3. validation evidence
4. rollback note, if applicable
