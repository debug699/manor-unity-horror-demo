# 《庄园》第一版 Demo：AI 美术生产与 Unity 制作任务书

版本：1.0  
适用：Unity 6 + URP、Windows PC、第一人称恐怖生存逃脱 Demo

## 0. 任务书目的

本任务书把“概念图 → 参考包 → 3D 资产 → Unity 场景 → C# 玩法 → 验收”固定成一条流水线。

《庄园》的硬性边界：现代偏远乡村庄园；第一人称；约 22 分钟探索解谜 + 约 8 分钟屠夫追逐；无战斗、无武器、无养成、唯一结局；“法圣”为原创虚构邪教；写实、低饱和、潮湿腐败、冷灰绿环境、局部油灯暖光。

## 1. 软件顺序和职责

| 顺序 | 软件 | 负责内容 | 何时交给 Codex |
|---|---|---|---|
| 1 | 项目 Docs + Git/Git LFS | 冻结设定、目录、版本和授权记录 | 先让 Codex 建目录、命名和检查清单 |
| 2 | Midjourney 或 ImageGen | 探索庄园、房间、光照和总体氛围；只产概念参考 | 母图选定后，让 Codex 登记参考图 |
| 3 | ComfyUI | 固定模型、种子、参考图，批量生成统一的环境、道具、材质参考 | 每个参考包验收后，让 Codex 搭对应 Unity 区域 |
| 4 | Krita + ImageMagick | 裁剪、去背景、拼参考板、处理贴图/Decal/UI | PNG/TGA 通过后，让 Codex 创建 Unity 材质或 UI |
| 5 | Blender | 模块化建筑、道具、角色模型清理、UV、减面、FBX 导出 | 模型通过比例和碰撞检查后，让 Codex 建 Prefab |
| 6 | Unity 6 + URP | 灰盒、材质、灯光、NavMesh、交互、剧情、怨灵、屠夫追逐 | 每个功能只给 Codex 一个小任务并测试 |
| 7 | OBS + Unity Recorder + Audacity | 截图、录像、音频初步处理、验收反馈 | 把截图/录像/日志交给 Codex 分类修复 |

不要让 Midjourney、ComfyUI 或 Codex 直接“生成完整 Unity 游戏”。图片负责视觉参考，Blender 负责可用模型，Unity 负责可玩场景，Codex 负责连接代码和验证。

## 2. 统一风格底座

所有场景提示词都必须保留下面这段，只替换场景专用内容：

```text
realistic first-person horror game concept art for an original fictional rural manor,
modern remote countryside setting, grounded believable architecture, damp decay,
low-saturation grey-green and cold grey palette, dark aged wood, moldy plaster,
rusty metal, stained paper and worn cloth, restrained warm oil-lamp pools,
soft volumetric mist, readable navigation and believable scale, cinematic but practical,
quiet dread, sorrowful folk horror, realistic materials, subtle grime and limited dark-red stains.
No cartoon style, no fantasy magic, no cyberpunk, no neon colors, no modern luxury mansion,
no real-world religious symbols, no copyrighted characters, no watermark, no readable text.
Blood and gore must be restrained: stains and traces only, no dismemberment, no splatter wall,
no exposed organs, no shock-gore composition.
```

统一负面词：`cartoon, anime, cute, high saturation, neon, fantasy spell effects, superhero, modern luxury villa, castle, palace, real religious iconography, random text, watermark, excessive gore, dismemberment, exposed organs, distorted perspective, floating props, impossible architecture, clean new materials`。

## 3. 目录和归档

```text
D:\游戏制作软件\庄园\
├─Assets\_Project\ArtReferences\
│  ├─00_StyleBible\              母图、固定提示词、色板、反面参考
│  ├─01_Environment\G01-G12\B01-B02\O01-O08\
│  ├─02_Characters\Tom\PregnantWraith\ChildEchoes\Butcher\
│  ├─03_Props\                   钥匙、日记、羊皮纸、锁魂钉、祭器、木板等
│  ├─04_Materials\               木、墙、石、金属、纸、布、Decal
│  └─05_UI\
├─Assets\_Project\Art\Environment\Characters\Props\Materials\UI\
├─Assets\_Project\Scenes\SCN_ManorDemo_庄园Demo.unity
├─Assets\_Project\Prefabs\Environment\Characters\Gameplay\UI\
├─Assets\_Project\Scripts\Core\Gameplay\AI\Narrative\Editor\Tests\
└─Docs\AI_Asset_Log.md / ASSET_LICENSES.md / Visual_Review_Reports\
```

每张图片旁边必须有同名 `notes.md`：生成软件、日期、模型/工作流、Seed、参考图编号、用途、商用许可、必须保留内容、禁止改动内容。

## 4. 阶段一：先生成五张风格母图

软件：Midjourney 或 ImageGen。目的：只确定方向，不直接进入 Unity。每张最多保留两个候选。

### 4.1 庄园外观

归档：`Assets/_Project/ArtReferences/00_StyleBible/Environment_Manor_Exterior/`

```text
[统一风格底座]
A two-storey old rural manor in a remote modern countryside, viewed from the muddy front approach,
small enclosed yard, rotten fence, wet roof, boarded windows, weak warm light behind one window,
heavy overcast sky and thin rain mist, believable practical building scale, no castle and no palace.
Wide establishing shot, eye-level first-person reference, clear silhouette and entrance route.
```

### 4.2 主楼走廊

归档：`Assets/_Project/ArtReferences/00_StyleBible/Main_Hallway/`

```text
[统一风格底座]
First-person view inside the manor main hallway, narrow low ceiling, damp grey-green plaster,
aged dark wood trim, warped floorboards, old doors and a stairwell, one weak oil lamp creating
a restrained warm pool while the far corridor remains cold and readable, subtle child toy at the edge,
no creature face in the center, practical gameplay composition, 24mm eye-level lens.
```

### 4.3 地下祭祀室

归档：`Assets/_Project/ArtReferences/00_StyleBible/Underground_Ritual_Room/`

```text
[统一风格底座]
A cramped underground ritual chamber beneath an old rural manor, wet stone walls, low ceiling,
central original fictional ritual circle made of dark-red abstract symbols, four clear sacrifice positions,
old altar, rusted lock-soul nails, ash, torn cloth, broken wooden idol with no real religious identity,
small pale blue-green spirit glow, cold mist, visible investigation objects, realistic scale and surfaces.
Symmetrical but oppressive composition, no readable text, no real-world religious symbols.
```

### 4.4 室外追逐环线

归档：`Assets/_Project/ArtReferences/00_StyleBible/Outdoor_Chase_Route/`

```text
[统一风格底座]
A small enclosed manor yard leading through two rotten sheds, debris piles, broken fence,
wooden board obstacles, a damaged bridge and a distant manor gate, wet ground and fog,
several readable cover routes for a first-person chase, restrained moonless blue-grey night,
subtle warm light leaking from the back door, no open-world forest, no wide empty landscape.
```

### 4.5 材质和灯光板

归档：`Assets/_Project/ArtReferences/00_StyleBible/Material_Lighting_Sheet/`

```text
[统一风格底座]
A realistic material reference sheet for the manor game: damp cracked wood, moldy plaster,
rusty metal, stained yellow paper, worn grey cloth, wet stone, ash and restrained dark-red stains,
shown under cold grey-green ambient light and a small warm oil lamp, close-up but not abstract,
clear roughness and moisture variation, no text, no clean new surfaces.
```

## 5. 阶段二：ComfyUI 批量生成统一参考包

ComfyUI 的职责是延续风格，不是马上做最终模型。先固定一个 SDXL 模型、Sampler、Steps、CFG、尺寸和 Seed；使用母图作为 IP-Adapter 参考，使用灰盒截图或平面草图作为 ControlNet 构图参考。你的 RTX 4060 Laptop 建议先用 SDXL，不要一开始使用重型工作流。

### 5.0 先生成整体平面布局参考

这一组图必须优先于房间氛围图和 Unity 灰盒。平面图的任务是确认“房间之间怎么连接、玩家怎么走、哪里能绕路、地下入口是否隐藏”，不是用来直接生成漂亮宣传图。AI 生成的文字和尺寸通常不可靠，所以图中不要要求 AI 写房间名称；生成后用 Krita 或 PowerPoint 手工编号，再交给 Codex。

统一布局负面词：`perspective view, isometric view, 3D render, cinematic lighting, decorative fantasy map, random text, unreadable labels, extra rooms, missing rooms, impossible corridors, disconnected doors, circular maze, open world, castle, palace`。

#### 5.0.1 一层平面布局

归档：`Assets/_Project/ArtReferences/00_StyleBible/Layout_GroundFloor/`

```text
Top-down orthographic architectural floor plan reference for a realistic first-person horror game,
old two-storey rural manor ground floor, practical human-scale proportions, clear wall thickness,
doors, stairs, windows, furniture footprints and walkable corridors, no perspective, no 3D render.
Include exactly these connected areas: entrance hall, living room suite, kitchen suite,
butcher workroom, main hallway, stairwell, rear passage.
The entrance hall connects to the living room suite and main hallway.
The living room connects to the kitchen and main hallway.
The kitchen connects to the butcher workroom and has a concealed service-area clue near the back.
The butcher workroom connects to the rear passage.
The stairwell connects ground floor and upper floor.
The rear passage leads to the small courtyard.
Leave readable hiding and sightline-break opportunities in the hallway and rear passage.
Use a clean monochrome architectural diagram with subtle damp old-manor texture,
no room labels, no invented extra rooms, no real-world religious symbols.
```

#### 5.0.2 二层平面布局

归档：`Assets/_Project/ArtReferences/00_StyleBible/Layout_UpperFloor/`

```text
Top-down orthographic architectural floor plan reference for a realistic first-person horror game,
old rural manor upper floor, practical human-scale proportions, clear walls, doors, windows,
stair landing, furniture footprints and readable routes, no perspective, no 3D render.
Include exactly these connected areas: pregnant woman's bedroom suite, divided children's room,
butcher research room, fictional Fasheng prayer room, old guest room and sealed transition space,
plus the upper stair landing.
The upper stair landing connects to the pregnant woman's room, children's room, research room,
prayer room and old guest room. The research room is near the prayer room.
The children's room must be one larger room divided into two different family zones,
not a twin bedroom and not a family nursery. Keep one short chase loop and at least two
line-of-sight breaks, but do not create a maze or any extra main room.
Use a clean monochrome architectural diagram with subtle damp old-manor texture,
no room labels, no invented extra rooms, no real-world religious symbols.
```

#### 5.0.3 地下与室外布局

归档：`Assets/_Project/ArtReferences/00_StyleBible/Layout_Basement_Outdoor/`

```text
Top-down orthographic site and basement layout reference for a realistic first-person horror game.
Show the relationship between the approved two-storey rural manor, hidden basement service route,
underground ritual chamber, small courtyard, butcher work shed, abandoned storage shed,
debris and rotten fence area, board obstacle and window shortcut, damaged bridge,
gate road and manor gate.
The hidden basement entrance is concealed near the kitchen back service area.
The ritual chamber is beneath and behind the main house near the butcher workroom,
not directly visible from the entrance hall or main hallway.
The outdoor areas form a readable chase loop from the rear passage to the courtyard,
sheds, debris, bridge and gate road. Use clear walkable routes and cover points.
No open-world forest, no extra buildings, no perspective, no 3D render, no random text.
```

#### 5.0.4 平面图使用流程

1. 用 Midjourney、ImageGen 或 ComfyUI 生成一层、二层、地下/室外三张平面参考。
2. 选出结构最接近项目布局母版的版本，不要因为画面漂亮就选择结构错误的版本。
3. 在 Krita 中手工添加 `G01-G12`、`B01-B02`、`O01-O08` 编号、箭头、门和追逐路线。
4. 保存为 `layout_ground_floor_approved.png`、`layout_upper_floor_approved.png`、`layout_basement_outdoor_approved.png`。
5. 把三张图和 LayoutBible 一起交给 Codex，先生成 Unity 灰盒。

### 5.1 每个房间的俯视布局参考提示词

下面每条都要加“统一布局负面词”。提示词生成的是“房间布局参考图”，不是最终氛围图。每个房间建议输出：俯视图 1 张、玩家入口视角 1 张、关键调查点细节 1 张。

通用开头：

```text
Top-down orthographic room layout reference for a realistic first-person horror game,
human-scale old rural manor interior, clear walls, doors, windows, furniture footprints,
walkable area, investigation points and one readable player entry direction, no perspective,
no 3D render, no room labels, no invented extra room, preserve the approved layout relationship.
```

#### G01-G07：一层

| 区域 | 房间布局专用提示词 | 布局重点 |
|---|---|---|
| G01 玄关 | `compact entrance hall, front door, interior gate line, coat hooks, worn table with first clue, clear route to living room and main hallway, no dead end` | 正门入口、方向教学、两个出口；不是开场出生点 |
| G02 客厅套间 | `connected living room, dining corner and old reception area, sofa and table footprints, covered windows, furniture creating one short sightline break, two connections to entrance and main hallway` | 生活痕迹、客厅到走廊的路线 |
| G03 厨房套间 | `connected kitchen, pantry, washing area and narrow service passage, sink, shelves, drainage channel, concealed floor trapdoor behind storage, connection to living room, butcher room and main hallway` | B01 活板门、后勤通道、冷风和排水异常 |
| G04 屠宰工作间 | `butcher workroom with cutting table, tool wall, drain channel, repeated heavy drag marks, lime dust and wood shavings, separate door to kitchen, clutter-blocked rear door that can be cleared, no combat arena` | 职业身份、拖痕、犯罪痕迹、后门路线 |
| G05 主走廊 | `long narrow main hallway with several doors, one central visual anchor, two doorframe sightline breaks, small hiding recess, connection to entrance, living room, kitchen, stairwell and rear passage` | 方向记忆、怨灵活动、短距离躲避 |
| G06 楼梯间 | `L-shaped stairwell connecting ground and upper floor, landing, sealed and abandoned disguised service stair detail, safe turning radius for first-person movement` | 上下层关系、废弃旧地下入口 |
| G07 后门通道 | `narrow rear passage from butcher workroom and stairwell to a back door, stacked clutter creating partial cover, clear exit to courtyard, one alternate sightline, no maze` | 室内转室外、追逐转换点 |

#### G08-G12：二层

| 区域 | 房间布局专用提示词 | 布局重点 |
|---|---|---|
| G08 孕妇房间套间 | `bedroom, small clothing area and wash corner in one connected suite, diary hidden beneath bed, crawl-under hiding space, hidden parchment fragment location, one narrow exit to upper landing, no sexualized imagery` | 床底日记、羊皮纸碎片、怨灵追逐发现、悲剧叙事 |
| G09 儿童房双区 | `one large children's room divided into two distinct living zones from different village families, separate beds, separate toy areas, separate storage and markings, central narrow passage, two possible sightline breaks, not twins, not a cute nursery` | 两个不同家庭、遗物和涂鸦 |
| G10 屠夫研究室 | `larger archive study with desk, locked cabinets, wall records, no secret room, research-room key, failed adult-male records, vague ritual-chamber evidence, floor clearance for investigation, connection near the prayer room` | 信息密度最高、研究室钥匙、地下阵室侧面线索、桥梁记录 |
| G11 祷告室 | `small side room near the research study where the player wakes at game start, simple altar, original Fasheng wooden idol, ash area, iron wire, key near the idol, torn cloth, original fictional symbol space, one narrow entrance, no real-world religious layout` | 开场醒来、铁丝撬锁、研究室钥匙、虚构法圣符号 |
| G12 旧客房与过渡空间 | `abandoned upstairs room of the butcher's nearly adult son, diary, late-teen belongings, old bed, wardrobe, traversable but uncomfortable narrow transition corridor, one hiding corner and one alternate route, compact and practical, not a new major wing` | 儿子失败实验叙事、绕路、声音误导、短暂躲避 |

#### B01-B02、O01-O08：地下和室外

| 区域 | 房间布局专用提示词 | 布局重点 |
|---|---|---|
| B01 隐藏地下入口 | `concealed service stair behind kitchen storage, partial stairs, narrow landing, drainage mark, crates and moldy cloth, hidden from main hallway, connection toward the underground ritual chamber` | 隐藏入口、线索逐步汇集 |
| B02 地下祭祀室 | `compact rectangular underground chamber, central ritual circle, four clearly separated sacrifice positions at four directions, altar, parchment position, wall niches, investigation path around the center, one controlled entrance, no boss arena` | 四个祭品位置、法阵、完整羊皮纸 |
| O01 庭院 | `small enclosed courtyard connected to the rear passage, back door, muddy open center, low cover near walls, routes to two sheds and debris area` | 室内出口、三向分流 |
| O02 屠夫工作木屋 | `small butcher work shed with workbench, storage shelves, exterior escape side, narrow but navigable interior, one window route, no combat arena` | 屠夫身份、翻窗路线 |
| O03 废弃储物木屋 | `small abandoned storage shed with broken shelves, crawl-sized visual cover, two exits, old evidence object, readable chase loop connection` | 绕路、躲避、罪证 |
| O04 杂物堆与围栏 | `debris pile and rotten fence zone, narrow passage, stacked timber, partial cover, one route toward board obstacle and one route back to courtyard` | 遮挡、窄路、脱离视线 |
| O05 木板障碍与翻窗点 | `wooden board obstacle and designated window shortcut, clear approach and landing area, believable height, no free climbing system, route continues toward bridge` | 砸板、翻窗、追逐捷径 |
| O06 断桥与桥梁机关 | `damaged bridge crossing with a visible but unreachable gate road, bridge control mechanism nearby, safe player stopping area, alternate return route before dawn` | 法阵崩溃后切断、黎明后恢复 |
| O07-O08 大门道路与庄园大门 | `narrow gate road from the bridge to the manor gate, gatehouse-like entrance without castle architecture, keypad or password interaction point, final escape position, clear dawn sightline` | 唯一逃生终点、密码输入 |

### 5.2 场景通用模板

归档：`Assets/_Project/ArtReferences/01_Environment/GXX_空间名/`

```text
[统一风格底座]
Environment reference for the original manor game, area [GXX NAME].
Preserve the approved two-storey rural manor layout and first-person gameplay scale.
Show [AREA-SPECIFIC FUNCTION], [KEY CLUE], [ROUTE OR HIDING FEATURE].
Camera at human eye height, readable doors and paths, practical room proportions,
realistic materials, no extra main room, no impossible corridors, no random readable writing.
Composition: one wide gameplay view plus one detail view of the clue object.
```

### 5.3 B01 隐藏地下入口

归档：`Assets/_Project/ArtReferences/01_Environment/B01_Hidden_Entrance/`

```text
[统一风格底座]
A concealed underground service entrance behind a cluttered kitchen back area,
old shelves, crates, moldy cloth, drainage stain, a faint cold draft and a narrow disguised stair,
clues are subtle and believable, the underground chamber is not visible from the main hallway,
first-person investigation composition, no obvious fantasy portal, no new room beyond the layout bible.
```

### 5.4 B02 地下祭祀室

归档：`Assets/_Project/ArtReferences/01_Environment/B02_Ritual_Chamber/`

```text
[统一风格底座]
The approved underground ritual chamber B02, central original fictional ritual circle,
four distinct sacrifice positions, altar, lock-soul nails, old ritual objects, fragments of parchment,
wet stone, mold, ash, torn cloth, restrained dark-red traces and a pale blue-green spirit presence.
The room must be oppressive but investigation-readable; no real religion, no readable text,
no excessive gore, no oversized boss arena, no random props blocking the route.
```

### 5.5 室外区域模板

归档：`Assets/_Project/ArtReferences/01_Environment/O01-O08/`

```text
[统一风格底座]
Outdoor chase route for [O AREA NAME] in the approved manor layout.
Include [CHASE FUNCTION], [COVER OR OBSTACLE], [NEXT ROUTE CONNECTION].
Wet ground, rotten wood, limited visibility but readable navigation, realistic first-person scale,
no open-world forest, no random dead ends, no combat arena, no heroic composition.
```

每个区域至少输出：一张宽景、一张玩家视角、一张关键角落细节、一张路线/构图参考。

## 6. 阶段三：Krita/ImageMagick 处理为 Unity 资源

| 资源 | 处理 | 输出 | Unity 用途 |
|---|---|---|---|
| 木/墙/石 | 裁剪、接缝处理、基础色/法线/粗糙度辅助 | `Assets/_Project/Art/Materials/Environment/` | URP Lit 材质 |
| 血迹/霉斑/水痕 | 透明 PNG，边缘柔和，血迹只做有限线索 | `Assets/_Project/Art/Materials/Decals/` | Decal/贴花 |
| 日记/羊皮纸 | AI 只生成纸张底图，最终文字用 TMP | `Assets/_Project/Art/UI/ReadingPages/` | 阅读界面 |
| UI | 旧纸、灰黑、暗红、低饱和；文字必须清晰 | `Assets/_Project/Art/UI/` | Canvas、按钮、死亡/通关界面 |
| 参考板 | 拼图、编号、notes.md，不进入运行时包 | `ArtReferences/` | 给 AI 和人工制作参考 |

## 7. 阶段四：Blender 建模顺序

先建筑模块：墙、地板、门框、楼梯、木板、窗、围栏、断桥；再关键道具：钥匙、日记、羊皮纸、锁魂钉、祭坛、木雕、玩具、门锁、桥梁机关；最后角色：汤姆、孕妇怨灵、屠夫。儿童第一版优先用残影、玩具、脚步和窗口身影，不强制制作完整儿童 AI 模型。

```text
Asset: [名称]
Scale: real-world meter scale; door height about 2.0 m; first-person readable silhouette
Materials: [wood/plaster/stone/metal/paper/cloth]
Damage: dampness, mold, rust, wear; restrained dark-red trace only when story requires
Geometry: game-ready, clean normals, no hidden internal faces, pivot at practical hinge/base
Deliverables: .blend source, .fbx export, preview.png, notes.md, license/source record
```

每个模型必须检查：真实比例、原点、UV、材质槽、碰撞体、命名、面数、FBX 导出和近距离第一人称可读性。

## 8. 阶段五：什么时候交给 AI 写 Unity

### A. 先做灰盒布局

输入：空间布局母版、GDD、TechnicalDesign。输出：`SCN_ManorDemo`、G01-G12/B01-B02/O01-O08 占位几何、玩家出生点、碰撞、路线、NavMesh。此阶段不要正式美术。

### B. 灰盒通过后做玩家和交互

输出：第一人称移动、奔跑、蹲伏、视角、开门、拾取、阅读、柜子、翻窗、木板交互。此阶段仍不做正式模型。

### C. 再做剧情状态

输出：`StoryStateService`、目标、线索、钥匙、羊皮纸沟通、存档、法阵崩溃、唯一结局流程。

### D. 再做怨灵演出和屠夫追逐

孕妇怨灵：有限状态 AI、声音预警、追逐和挣脱；儿童：残影、玩具、脚步、涂鸦和窗口身影；屠夫：后半段追逐、躲藏、断桥、黎明石化。禁止生成战斗、武器和伤害数值。

### E. 最后替换正式美术

只有参考图、模型、贴图和材质通过你的验收后，才让 Codex 创建 Prefab、替换占位物、设置灯光、NavMesh 和场景引用。

### F. 最后做测试和构建

输出：自动测试、截图点、Console 检查、1920×1080 和 2560×1600 验证、Windows 构建、日志和已知问题清单。

## 9. Codex 三个可复制任务提示词

### 9.1 灰盒布局

```text
你是《庄园》Unity 6 URP 项目的实现工程师。
先读取：项目背景总纲、02_游戏设计文档_GDD.md、06_技术设计文档_TechnicalDesign.md、08_庄园空间布局母版_LayoutBible.md。
本次只完成：在 SCN_ManorDemo 中创建 G01-G12、B01、B02、O01-O08 的灰盒布局。
保持第一人称、无战斗、无武器、唯一结局、12 个地面编号空间和既定追逐路线。
不要生成正式美术，不要增加房间，不要增加战斗，不要修改剧情。
完成后执行 Unity 编译/验证，报告修改文件、场景层级、碰撞、NavMesh、Console 错误和截图点。
```

### 9.2 单一功能

```text
在已有灰盒通过的 SCN_ManorDemo 上实现本次功能：[只填写一个功能]。
读取项目总纲、GDD、TechnicalDesign 和 Acceptance 文档。
当前状态：[客观描述]；目标状态：[客观描述]。
保持不变：[移动、布局、剧情、输入按键等]。
禁止：[战斗、武器、数值成长、瞬移、穿墙、随机跳出敌人]。
先列出修改文件和影响范围，再实施；完成后编译、运行对应 Test Scene、检查 Missing Reference、保存日志。
```

### 9.3 正式美术替换

```text
把已验收的资源包导入 Unity，并替换 [区域/Prefab] 的占位资产。
输入素材：[参考图路径] [模型路径] [贴图路径]。
禁止改动：[碰撞、路线、交互点、剧情触发器]。
保持低饱和灰绿/冷灰环境、局部油灯暖光、真实潮湿材质和第一人称可读性。
完成后检查比例、碰撞、材质丢失、粉色 Shader、灯光、FPS、截图和 Console。
```

## 10. 场景生产清单和专用提示词

每条提示词都要加“统一风格底座”。

| 区域 | 用途 | 场景专用词 |
|---|---|---|
| G01 玄关 | 正门入口、方向教学 | `old entrance hall, muddy threshold, locked manor door, weak rain light, one clue note on a worn table, readable route to living room and main hallway, not a spawn room` |
| G02 客厅套间 | 生活痕迹、家庭线索 | `old living room and dining suite, damp furniture, objects from different households, covered windows, quiet clue corner` |
| G03 厨房套间 | 活板门、排水声、后勤线索 | `damp rural kitchen, old sink, stained drainage channel, pantry shelves, partially concealed floor trapdoor behind clutter, faint cold draft from concealed service route` |
| G04 屠宰工作间 | 屠夫职业、拖痕、少量血迹 | `realistic butcher workroom, heavy cutting table, drains, rusted tools as identity props, repeated heavy drag marks, lime dust, wood shavings, restrained old stains, cold storage, clutter-blocked rear door` |
| G05 主走廊 | 方向锚点、怨灵预警 | `narrow hallway, warped boards, old doors, one oil lamp, distant child toy, sightline breaks, not a maze` |
| G06 楼梯间 | L 形上下层、废弃地下楼梯 | `old L-shaped stairwell, cluttered landing, sealed abandoned disguised service stair, low ceiling, readable vertical route` |
| G07 后门通道 | 室内转室外 | `narrow rear passage, damp back door, stacked objects, limited sightline, clear exit to courtyard` |
| G08 孕妇房间 | 床底日记、羊皮纸碎片 | `plain old rural pregnant woman bedroom suite, worn clothes, diary hidden beneath bed, crawl-under hiding space, wash area, sorrowful traces, non-sexualized` |
| G09 儿童房双区 | 两个不同家庭的遗物 | `one room divided into two distinct living zones from different families, different toys and markings, not twins, not cute` |
| G10 研究室 | 法圣记录、失败记录、阵室侧面线索 | `larger old study, locked cabinets, original fictional symbols, failed adult-male records, vague ritual-chamber evidence, bridge mechanism notes, no secret room, no readable AI text` |
| G11 祷告室 | 开场醒来、铁丝、原创木雕 | `small fictional Fasheng prayer room where Tom wakes at game start, original abstract symbols, original carved wooden idol, ash, iron wire, key near idol, torn cloth, dim red traces, no real religion` |
| G12 旧客房 | 儿子房间、绕路、声音误导 | `abandoned upstairs room of the butcher's nearly adult son, diary and late-teen belongings, traversable uncomfortable transition space, broken wardrobe, sound-based hiding route, not a major new area` |
| B01 隐藏入口 | G03 地板活板门后地下服务路线 | `concealed basement service entrance through a floor trapdoor behind kitchen storage clutter, drainage stain, cold air, partial stairs, narrow underground route toward B02, hidden from main hall` |
| B02 祭祀室 | 四个位置、白骨、锁魂钉、完整羊皮纸 | `large and imposing wet underground ritual chamber, original fictional symbols, four sacrifice positions, pregnant woman and two yin-child stable positions, failed adult-male position, bodies fixed by lock-soul nails, visible bones and old blood, failed adult-male bones scattered along walls and perimeter, altar, parchment, readable investigation route, no dismemberment` |
| O01-O08 室外路线 | 庭院、木屋、翻窗、断桥、大门 | `enclosed manor yard, rotten sheds, debris, broken fence, board obstacles, window shortcut, damaged bridge, distant gate, loopable chase route` |

## 11. 验收和回滚

每批资产、每次 Codex 修改都按此闭环：

1. 你确认参考图/模型方向。
2. 创建 Git 版本点并记录输入素材。
3. 只执行一个软件任务或一个代码任务。
4. 编译、运行 Test Scene 或 Play Mode。
5. 检查粉色材质、穿模、漂浮、路线、亮度、雾、血迹尺度、UI 可读性、FPS 和 Console。
6. 保存前后截图和日志。
7. 你选择“通过 / 小改 / 回滚”。通过后才进入下一步。

反馈模板：

```text
问题类型：[代码 / 场景 / 模型 / 材质 / 灯光 / AI / UI / 性能]
位置：[场景、区域、对象]
当前结果：[客观描述]
目标结果：[可观察的改变]
参考图：[路径；只参考哪一部分]
保持不变：[布局、碰撞、剧情、角色身份、颜色]
禁止出现：[具体内容]
验收标准：[截图和运行时应该看到什么]
请先给出修改计划和影响文件，再实施。
```

## 12. 新手最简执行顺序

1. 先用 Midjourney 或 ImageGen 选定五张风格母图。
2. 用 ComfyUI 生成 G01 玄关、G05 主走廊、B02 地下祭祀室三组参考图。
3. 让 Codex 做完整灰盒布局和玩家移动/交互。
4. 你在 Unity 里试玩灰盒，确认路线和恐怖节奏。
5. 用 Blender/Krita 处理 G01、G05、B02 的建筑和关键道具。
6. 让 Codex 把已通过资产替换进 Unity，完成材质、灯光和碰撞。
7. 做一次 5-10 分钟垂直切片，通过后再批量制作剩余房间。

最重要的分工：图片软件负责“长什么样”；Blender 负责“变成可用模型”；Unity 负责“能不能走、看、交互和触发剧情”；Codex 负责“把它们接成可运行系统”。
