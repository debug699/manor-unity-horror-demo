# 《庄园》

Unity 6 + URP 单机第一人称 3D 恐怖生存逃脱 Demo。

## 项目目标

- Windows 64-bit
- 第一版 Demo 约 30 分钟
- 前 22 分钟探索、线索和羊皮纸沟通
- 后 8 分钟屠夫追杀和黎明生存
- 无战斗、无武器、唯一逃生结局

## Unity 版本

Unity 6000.5.5f1（Unity 6.5）

## 命名规则

项目中的场景、对象和关键资源采用“英文标识 + 中文说明”，例如：

- `SCN_Boot_启动场景`
- `SCN_MainMenu_主菜单`
- `SCN_ManorDemo_庄园Demo`
- `PlayerSpawn_玩家出生点`

英文部分用于代码、搜索和稳定引用，中文部分用于帮助理解。

## 管理文档

项目策划、剧情、技术和验收文档位于：

`D:\游戏制作软件\管理\Docs`

新任务开始前，先阅读管理文档中的《庄园》项目背景总纲，再按任务选择对应详细文件。

## 自动化入口

Unity 菜单中可使用：`庄园 → 自动化`。

- 初始化项目：创建正式场景、测试场景和 Build Settings。
- 创建测试场景：创建独立测试场景框架。
- 验证项目：检查必要文件夹、场景和 Build Settings。
- 导出场景层级：输出场景结构到 `AutomationLogs/scene_hierarchy.txt`。
- 构建 Windows：输出 Windows 64 位版本到 `Builds/Windows/`。
