# 《庄园》Unity 项目协作说明

## 项目定位

这是 Unity 6.5 + URP 的 Windows 单机第一人称 3D 恐怖生存逃脱游戏《庄园》。第一版 Demo 约 30 分钟：前 22 分钟探索解谜，后 8 分钟屠夫追杀和黎明生存。

核心限制：无战斗、无武器、无击杀、无多人、无养成、无分支主线、唯一逃生结局。

## 项目路径

- Unity 工程：`D:\游戏制作软件\庄园`
- 项目管理文档：`D:\游戏制作软件\管理\Docs`
- GitHub：`https://github.com/debug699/my-first-3d-game.git`
- Git 分支：`main`
- Unity：`D:\游戏制作软件\Unity\6000.5.5f1\Editor\Unity.exe`

## 开始任何任务前

1. 阅读 `D:\游戏制作软件\管理\Docs\00_DOCS阅读索引_新聊天专用.md`。
2. 阅读 `D:\游戏制作软件\管理\Docs\《庄园》项目背景总纲_新聊天专用.md`。
3. 只根据任务选择对应详细文档，不默认读取全部 Docs 文件。
4. 修改前说明修改内容、影响文件和不会修改的内容。
5. 不得擅自加入战斗、武器、现实宗教、多结局或与项目背景冲突的角色关系。

## 项目目录

自有内容放在 `Assets/_Project/`。场景、脚本、预制体、数据和资源使用英文标识加中文说明，例如：

- `SCN_ManorDemo_庄园Demo.unity`
- `PlayerSpawn_玩家出生点`
- `PlayerController_玩家控制器.cs`

第三方资源放在 `Assets/ThirdParty/`，不得与自有资源混放。

## 项目专用 Skills（第 9 步）

项目专用 Skill 位于 `D:\游戏制作软件\庄园\.codex\skills\`。新聊天或更换模型时，先读本文件和 Docs 阅读索引，再按任务自动选择对应 Skill；不要为了了解项目而一次性阅读全部 Skill 或全部 Docs。

| Skill | 适用任务 | 任务开始时的最小文档阅读 |
|---|---|---|
| `manor-unity-automation` | Unity 初始化、场景、项目验证、自动化、测试 | 背景总纲 + `06_技术设计文档_TechnicalDesign.md`；涉及玩法再加 `02_游戏设计文档_GDD.md` |
| `manor-visual-acceptance` | 截图、灰盒、灯光、材质、UI、视觉验收 | 背景总纲 + `03_美术圣经_ArtBible.md`；涉及角色再加 `04_角色设定表.md` |
| `manor-character-assets` | 角色模型、图片、材质、骨骼、动画、Prefab | 背景总纲 + `04_角色设定表.md` + `03_美术圣经_ArtBible.md`；涉及剧情再加 `05_剧情圣经_StoryBible.md` |
| `manor-story-maintenance` | 剧情、时间线、线索、对白、伏笔、环境叙事 | 背景总纲 + `05_剧情圣经_StoryBible.md`；涉及玩法再加 `02_游戏设计文档_GDD.md` |
| `manor-horror-chase` | 无战斗恐怖演出、怨灵、躲藏、屠夫追逐、黎明生存 | 背景总纲 + `05_剧情圣经_StoryBible.md` + `02_游戏设计文档_GDD.md` + `06_技术设计文档_TechnicalDesign.md` |
| `manor-windows-release` | Windows 验证、构建、打包、冒烟测试、发布 | 背景总纲 + `06_技术设计文档_TechnicalDesign.md` + `07_验收流程与验收标准_Acceptance.md` |

当前工程的旧白膜与已确认布局冲突，已放弃作为新工程依据并保存在备用文件夹；后续按确认后的关系重新开始。新布局固定为：G11 祷告室开场醒来，F 拾取、E 普通交互、K 按住 5 秒使用；G03 厨房后勤区地板活板门进入 B01，G06 旧地下入口废弃，G04 通过拖痕和可清理后门提供线索与室外路线；G10 与 G11 相邻，G12 为屠夫儿子早期实验失败后的旧房间和狭窄过渡空间；B02 展示四个位置、白骨、血迹和锁魂钉痕迹，失败成年男性白骨分散在墙边和外围。固定油灯和有限药物属于探索辅助，不显示数字血条。精确拓扑以 `D:\游戏制作软件\管理\Docs\08_庄园空间布局母版_LayoutBible.md` 为准。真正进入 Unity/C# 玩法实现时，必须先说明将修改的文件和影响范围；若用户要交给新的代码模型，应提供完整任务提示词和相关文档路径。

## 自动化命令

```powershell
# 初始化/修复基础场景和 Build Settings
Unity.exe -batchmode -quit -projectPath "D:\游戏制作软件\庄园" -executeMethod Manor.Editor.ManorProjectAutomation.Initialize -logFile "D:\游戏制作软件\庄园\AutomationLogs\initialize.log"

# 检查项目结构、场景、包和脚本错误
Unity.exe -batchmode -quit -projectPath "D:\游戏制作软件\庄园" -executeMethod Manor.Editor.ManorProjectAutomation.ValidateProject -logFile "D:\游戏制作软件\庄园\AutomationLogs\validate.log"

# 创建/更新独立测试场景
Unity.exe -batchmode -quit -projectPath "D:\游戏制作软件\庄园" -executeMethod Manor.Editor.ManorProjectAutomation.CreateTestScenes -logFile "D:\游戏制作软件\庄园\AutomationLogs\tests.log"

# 构建 Windows 64 位版本
Unity.exe -batchmode -quit -projectPath "D:\游戏制作软件\庄园" -executeMethod Manor.Editor.ManorProjectAutomation.BuildWindows -logFile "D:\游戏制作软件\庄园\AutomationLogs\build.log"
```

## 修改后必须检查

- Unity Console 没有阻止运行的错误。
- 没有 Missing Script 或严重 Missing Reference。
- 场景能加载，Build Settings 顺序正确。
- 输入、存档、剧情状态、AI 和交互修改后运行对应测试。
- Windows 构建可以独立启动。
- 每次重要修改后保留日志，并在 Git 中建立可恢复版本。
