using System;
using System.IO;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Loads once, caches in memory, writes JSON to persistentDataPath.</summary>
    public static class SaveSystem
    {
        const string FileName = "save.json";
        static SaveData data;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static event Action Changed;

        public static void Load()
        {
            try
            {
                data = File.Exists(FilePath) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save file could not be read, starting fresh: {e.Message}");
                data = null;
            }
            data ??= new SaveData();
            data.Settings ??= new SettingsData();
            data.Upgrades ??= new System.Collections.Generic.List<UpgradeLevelEntry>();
            data.UnlockedSkills ??= new System.Collections.Generic.List<string>();
        }

        public static void Save()
        {
            if (data == null) return;
            try
            {
                var tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Save failed: {e.Message}");
            }
            Changed?.Invoke();
        }

        /// <summary>For tests: replace the in-memory data without touching disk.</summary>
        public static void SetDataForTests(SaveData value) => data = value;
    }
}
