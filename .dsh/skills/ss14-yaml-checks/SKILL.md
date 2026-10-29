---
name: ss14-yaml-checks
description: Static checks for SS14 prototypes, Fluent, RSI, ECS sides.
whenToUse: After YAML/C# content changes; when running tester-reviewer.
---

# Checks

## Prototypes
- Stable ids; parents exist; no dangling refs
- Species/markings/loadouts: required fields and linked prototypes present

## Locale
- Player strings in Resources/Locale (ru-RU for players)
- Keys match usage; Fluent args preserved

## RSI / textures
- States referenced by prototypes exist
- Paths under Resources/Textures consistent

## ECS
- Shared vs Server vs Client responsibilities
- No Client-authoritative gameplay state

## Report shape
block | warn | nit + path
