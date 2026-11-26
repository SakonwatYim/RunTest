using System;
using System.Collections.Generic;

namespace RunChildRun
{
    [Serializable]
    public sealed class UpgradeLevelEntry
    {
        public string Id;
        public int Level;
    }

    [Serializable]
    public sealed class SettingsData
    {
        public bool FixedJoystick;
        public float JoystickScale = 1f;
        public float JoystickOpacity = 1f;
        public bool ScreenShake = true;
        public bool Haptics = true;
        public float MusicVolume = 0.7f;
        public float SfxVolume = 1f;
    }

    /// <summary>Everything persisted between runs (saved as JSON).</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
        public int Coins;
        public float BestTime;
        public int BestLevel;
        public int BestKills;
        public int TotalRuns;
        public List<UpgradeLevelEntry> Upgrades = new List<UpgradeLevelEntry>();
        public List<string> UnlockedSkills = new List<string>();
        public SettingsData Settings = new SettingsData();

        public int GetUpgradeLevel(string id)
        {
            for (var i = 0; i < Upgrades.Count; i++)
            {
                if (Upgrades[i].Id == id) return Upgrades[i].Level;
            }
            return 0;
        }

        public void SetUpgradeLevel(string id, int level)
        {
            for (var i = 0; i < Upgrades.Count; i++)
            {
                if (Upgrades[i].Id == id)
                {
                    Upgrades[i].Level = level;
                    return;
                }
            }
            Upgrades.Add(new UpgradeLevelEntry { Id = id, Level = level });
        }
    }
}
