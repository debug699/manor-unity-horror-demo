---
name: manor-character-assets
description: Process Manor character references and assets with identity, outfit, spirit, scale, import, prefab, and consistency checks. Use when receiving character images, models, animations, materials, or preparing Tom, the pregnant wraith, child echoes, or the butcher for Unity.
---

# Manor Character Assets

Read the background guide first. Read `04_角色设定表.md` for appearance and production rules, `03_美术圣经_ArtBible.md` for visual rules, and `05_剧情圣经_StoryBible.md` only when the asset affects motivation, identity, or staging.

## Non-negotiable identities

- Tom is a modern private investigator and ordinary first-person protagonist.
- The pregnant wraith is a victim, not a sexualized monster or villain.
- The two child echoes are victims from different families, not twins and not the pregnant woman's children; Demo 1 uses echoes and environmental traces, not combat enemies.
- The butcher is a living active perpetrator, about 80 years old with a roughly 50-year-old appearance, not a tragic ghost or redeemable hero.

## Workflow

1. Record source, license, author, date, modification rights, and commercial-use status.
2. Check front/side/back references, scale, proportions, silhouette, materials, rig, animation, and texture paths.
3. Keep source assets under `Assets/ThirdParty/` and project-authored assets under `Assets/_Project/`.
4. Use English identifiers plus Chinese explanations for imported objects and prefabs.
5. Create a prefab only after import, scale, materials, rig, and missing-reference checks pass.
6. Capture an in-Unity preview and report objective defects separately from aesthetic questions.

Never import copyrighted characters, unlicensed real-person likenesses, or assets that conflict with project red lines.
