using System;
using System.IO;
using UnityEngine;

namespace Manor.Core
{
    public interface IAutoSaveService
    {
        string SavePath { get; }
        bool HasSave { get; }
        void Save(GameStateData state);
        bool TryLoad(out GameStateData state);
    }

    public sealed class JsonAutoSaveService : IAutoSaveService
    {
        private const string FileName = "manor_demo_save.json";
        private const string TemporaryFileName = "manor_demo_save.tmp";
        private const string BackupFileName = "manor_demo_save.bak";
        private readonly string _directory;

        public JsonAutoSaveService(string directory = null)
        {
            _directory = string.IsNullOrWhiteSpace(directory)
                ? Path.Combine(Application.persistentDataPath, "Saves")
                : directory;
        }

        public string SavePath => Path.Combine(_directory, FileName);
        public bool HasSave => File.Exists(SavePath);

        public void Save(GameStateData state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            Directory.CreateDirectory(_directory);
            GameStateData copy = state.Clone();
            copy.lastSaveUtc = DateTime.UtcNow.ToString("O");
            string json = JsonUtility.ToJson(copy, true);
            string temporaryPath = Path.Combine(_directory, TemporaryFileName);
            string backupPath = Path.Combine(_directory, BackupFileName);
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(SavePath))
            {
                File.Replace(temporaryPath, SavePath, backupPath, true);
                if (File.Exists(backupPath)) File.Delete(backupPath);
            }
            else
            {
                File.Move(temporaryPath, SavePath);
            }
        }

        public bool TryLoad(out GameStateData state)
        {
            state = null;
            if (!HasSave) return false;

            try
            {
                state = JsonUtility.FromJson<GameStateData>(File.ReadAllText(SavePath));
                return state != null && state.saveVersion > 0;
            }
            catch (Exception exception)
            {
                Debug.LogError("[Save] 自动存档读取失败 / Failed to read autosave: " + exception.Message);
                state = null;
                return false;
            }
        }
    }
}
