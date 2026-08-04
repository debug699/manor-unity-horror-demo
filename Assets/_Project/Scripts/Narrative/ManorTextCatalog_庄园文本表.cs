using System.Collections.Generic;

namespace Manor.Narrative
{
    public static class ManorTextCatalog
    {
        private static readonly Dictionary<string, string> Texts = new Dictionary<string, string>
        {
            { "OBJ_FIND_WHERE_I_AM", "弄清楚我在哪里" },
            { "OBJ_TEST_INTERACTIONS", "测试门、钥匙和纸条交互" },
            { "OBJECTIVE_PREFIX", "当前任务：" },
            { "PROMPT_OPEN_DOOR", "按 E 开门" },
            { "PROMPT_CLOSE_DOOR", "按 E 关门" },
            { "PROMPT_LOCKED_DOOR", "需要钥匙" },
            { "PROMPT_PICKUP_KEY", "按 E 拾取钥匙" },
            { "PROMPT_READ_NOTE", "按 E 阅读纸条" },
            { "CLUE_ADDED", "已记录新线索" },
            { "KEY_ADDED", "已获得钥匙" },
            { "DOOR_OPENED", "门已打开" },
            { "DOOR_CLOSED", "门已关闭" },
            { "NOTE_ALREADY_READ", "已读线索" },
            { "TEST_NOTE_TITLE", "测试纸条" },
            { "TEST_NOTE_BODY", "这是一张用于验证基础交互的灰盒纸条。阅读后会加入已读线索列表；它不属于正式剧情。" },
            { "TEST_CONTROLS", "WASD 移动　鼠标转视角　E 交互　Tab 线索　Esc 暂停/关闭" },
            { "CLUE_PANEL_TITLE", "线索记录" },
            { "CLUE_EMPTY", "尚未阅读测试纸条。" },
            { "CLUE_CLOSE_HINT", "按 Tab 或 Esc 返回" },
            { "PAUSE_TEXT", "游戏已暂停\n按 Esc 返回" },
            { "PAUSED", "游戏已暂停" },
            { "DEATH_TITLE", "汤姆未能逃脱" },
            { "RETRY", "重新开始" }
        };

        public static string Resolve(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return Texts.TryGetValue(key, out string value) ? value : key;
        }
    }
}
