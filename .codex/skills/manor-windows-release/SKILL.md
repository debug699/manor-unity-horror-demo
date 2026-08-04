---
name: manor-windows-release
description: Validate, build, package, smoke-test, and prepare Windows releases for the Manor Unity demo. Use when creating a development build, release candidate, final Windows executable, build report, or regression checklist.
---

# Manor Windows Release

Read the background guide first. Read `06_技术设计文档_TechnicalDesign.md` for build and performance rules and `07_验收流程与验收标准_Acceptance.md` before release decisions. Add GDD only when a build change affects gameplay.

## Release workflow

1. Check Git branch, working tree, Unity version, package lock, scenes, and project version.
2. Validate production scenes in order: `SCN_Boot_启动场景`, `SCN_MainMenu_主菜单`, `SCN_ManorDemo_庄园Demo`.
3. Check Console for blocking errors, Missing Scripts, Missing References, package failures, and shader failures.
4. Run EditMode/PlayMode tests available for the current milestone.
5. Build Windows 64-bit to `Builds/Windows/`.
6. Verify the executable starts outside the Unity Editor and reaches the first scene.
7. Test 1920x1080 and 2560x1600, input focus, pause, save/retry, and the current playable flow.
8. Record build version, Git commit, logs, known issues, and whether the build is pass, conditional pass, or fail.

## Release blockers

Do not release with a P0/P1 issue, broken scene loading, corrupt save, missing core package, unbuildable project, or a setting that introduces combat, weapons, multiple endings, or other canon-breaking content.
