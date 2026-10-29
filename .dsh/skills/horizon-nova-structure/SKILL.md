---
name: horizon-nova-structure
description: Horizon Nova SS14 layout and safe placement rules.
whenToUse: Feature start, unclear file placement, ports, multi-file work.
---

# Horizon Nova

## Layout
- Code: Content.Client | Shared | Server
- Data: Resources/Prototypes, Locale, Textures, Audio, Maps
- RobustToolbox = submodule; do not edit unless asked

## Placement
- Own content → Resources/**/_HorizonNova (+ Content.* if needed)
- Do not drop unique content into _Lua/_NF/_Goobstation/_Corvax* unless tasked

## Agent order (this profile)
searcher → (edit) → tester-reviewer → git-worker

## Defaults
No mass reformat; no force-push; no Server-only logic on Client.
