---
name: manor-unity-automation
description: Automate Unity 6.5 project initialization, scene creation, validation, logging, test setup, hierarchy export, and command-line workflows for the Manor project. Use when working on Unity project structure or editor automation, before gameplay-code implementation, or when checking a Manor project change.
---

# Manor Unity Automation

## Project contract

- Unity project: `D:\游戏制作软件\庄园`
- Unity executable: `D:\游戏制作软件\Unity\6000.5.5f1\Editor\Unity.exe`
- Documentation: `D:\游戏制作软件\管理\Docs`
- Branch: `main`
- Use Unity 6.5 + URP, Input System, AI Navigation, and Test Framework.
- Preserve modern setting, no combat, no weapons, and one ending.

Read the background guide first, then read `06_技术设计文档_TechnicalDesign.md` for technical tasks. Read `02_游戏设计文档_GDD.md` only when the task changes gameplay or scene rules.

## Workflow

1. Inspect Git status, Unity version, package manifest, scenes, and logs.
2. Explain planned files and scope before changing project structure.
3. Prefer the existing `ManorProjectAutomation` editor entrypoints.
4. Use Unity batchmode with `-projectPath`, `-executeMethod`, and `-logFile`.
5. Never edit `Library`, `Temp`, or generated package cache as source files.
6. After changes, validate folders, production scenes, Build Settings, package resolution, and compilation.
7. Report exact logs and remaining blockers.

## Naming

Use English identifiers plus Chinese explanations, such as `SCN_ManorDemo_庄园Demo`, `PlayerSpawn_玩家出生点`, and `TST_AI_追逐测试`.

## Do not

- Do not add combat, weapons, multiplayer, progression, or unrelated plugins.
- Do not claim a build passed without reading the build log and checking the output.
- Do not replace project assets or scenes destructively without explicit approval.
