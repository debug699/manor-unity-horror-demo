---
name: manor-horror-chase
description: Design and implement Manor non-combat horror staging, warning sounds, haunting events, hiding routes, environmental pressure, and butcher chase fairness. Use when creating or tuning scares, wraith encounters, child echoes, hiding, windows, boards, pursuit routes, or dawn survival.
---

# Manor Horror Chase

Read the background guide first. Read `05_剧情圣经_StoryBible.md` for narrative intent, `02_游戏设计文档_GDD.md` for gameplay rules, and `06_技术设计文档_TechnicalDesign.md` for implementation. Add `03_美术圣经_ArtBible.md` for lighting, VFX, and sound direction.

## Rules

- Horror must reveal story, teach a mechanic, change a route, or reinforce threat.
- Give sound, light, or environmental warning before major danger.
- The pregnant wraith uses finite patrol, investigate, chase, search, and return behavior; no wall phasing, random teleport, or infinite pursuit.
- Child echoes are Demo environmental traces, not combat enemies or direct killers.
- The butcher appears as a living pursuer only after ritual collapse; the goal is survival to dawn, never killing him.
- Hiding spots, windows, boards, walls, and sheds must provide understandable but imperfect options.
- The eight-minute chase is 480 seconds and must have at least one viable route at ordinary movement skill.
- Dawn stops the butcher and causes petrification; it does not start a second boss fight.

## Review

Test warning time, sight lines, sound direction, route readability, obstacle recovery, AI loss-of-target behavior, death fairness, and whether the player can understand why the threat is present.
