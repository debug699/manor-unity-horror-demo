using System.IO;
using UnityEngine;

namespace Manor.Runtime
{
    public sealed class ManorRuntimeLog : MonoBehaviour
    {
        private static ManorRuntimeLog instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (instance != null) return;
            GameObject root = new GameObject("RuntimeLog_运行日志");
            instance = root.AddComponent<ManorRuntimeLog>();
            DontDestroyOnLoad(root);
        }

        private void OnEnable() => Application.logMessageReceived += HandleLog;
        private void OnDisable() => Application.logMessageReceived -= HandleLog;

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                string directory = Path.Combine(Application.persistentDataPath, "ManorLogs");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "runtime_errors.log");
                File.AppendAllText(path, $"[{System.DateTime.UtcNow:O}] {type}: {condition}{System.Environment.NewLine}{stackTrace}{System.Environment.NewLine}");
            }
        }
    }
}
