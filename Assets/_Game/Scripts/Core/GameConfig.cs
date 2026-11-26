using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Every balance number for a run (GDD section 12). Distances and speeds are in GDD units
    /// ("หน่วย", 1920x1080 reference); convert with <see cref="ToWorld"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Run Child Run/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("World (GDD 3)")]
        [Tooltip("1 GDD unit = this many Unity units. 0.01 makes a 1920x1080 screen 19.2x10.8 units.")]
        public float UnitsPerGduUnit = 0.01f;
        public float ArenaHalfWidth = 2880f;
        public float ArenaHalfHeight = 1350f;
        [Tooltip("45-degree corner cut of the octagon, per side.")]
        public float ArenaCornerCut = 700f;
        [Tooltip("Visible height of the camera, constant on every device.")]
        public float ViewHeight = 1080f;

        [Header("Player (GDD 12)")]
        public float PlayerMaxHp = 100f;
        public float PlayerSpeed = 420f;
        public float PlayerRadius = 40f;
        public float DashDistance = 450f;
        public float DashDuration = 0.2f;
        public float DashCooldown = 6f;
        public float HitIFrames = 0.6f;
        public float LevelUpIFrames = 1.5f;
        [Tooltip("Enemies deal reduced damage during the first seconds of a run (GDD 7).")]
        public float EarlyDamageWindow = 20f;
        public float EarlyDamageMultiplier = 0.5f;

        [Header("EXP (GDD 4)  EXP_next(L) = Base + Linear*L + Quadratic*L^2")]
        public float ExpBase = 8f;
        public float ExpLinear = 6f;
        public float ExpQuadratic = 0.35f;
        public float TimeExpUnarmed = 1f;
        public float TimeExpArmed = 0.25f;
        public int LevelUpChoices = 3;
        public int RerollsPerRun = 1;
        [Tooltip("Card buttons are locked this long (real seconds) to avoid accidental taps.")]
        public float LevelUpSelectLock = 1f;
        [Tooltip("Max EXP feedback particles started per frame.")]
        public int MaxExpSparksPerFrame = 4;

        [Header("Items (GDD 5)")]
        public float FirstItemTime = 12f;
        public float SecondItemTime = 40f;
        public float RandomItemIntervalMin = 25f;
        public float RandomItemIntervalMax = 40f;
        public float ItemGroundLifetime = 45f;
        public float BadLuckUnarmedTime = 20f;
        [Tooltip("Fixed pickup distance between player edge and the gift box center.")]
        public float PickupRadius = 80f;
        public float[] ItemMinEnemyDistances = { 520f, 380f, 260f };
        public int ItemSpawnAttempts = 40;
        public float ItemRetryDelay = 1.5f;
        public float ItemMinPlayerDistance = 380f;
        public float ItemMaxPlayerDistance = 700f;
        public float ItemMaxAbovePlayer = 380f;
        public float ItemMinObstacleDistance = 250f;
        [Tooltip("The first N items lean towards attack skills.")]
        public int EarlySkillWeightedItems = 3;
        [Range(0f, 1f)] public float EarlySkillChance = 0.9f;
        public float SkillItemWeight = 70f;
        public float StatItemWeight = 22f;
        public float HealItemWeight = 8f;
        [Tooltip("Heal weight is multiplied by this when HP is below LowHpThreshold.")]
        public float LowHpHealWeightMultiplier = 3f;
        [Range(0f, 1f)] public float LowHpThreshold = 0.4f;
        [Range(0f, 1f)] public float HealAmountFraction = 0.3f;
        [Range(0f, 1f)] public float EliteItemDropChance = 0.25f;

        [Header("Rarity (GDD 5): Common / Rare / Epic")]
        public float[] RarityWeights = { 70f, 25f, 5f };
        public float[] RaritySkillDurations = { 45f, 60f, 75f };
        [Tooltip("Weight moved from Common to Rare / Epic per Luck level.")]
        public float LuckRareWeightPerLevel = 5f;
        public float LuckEpicWeightPerLevel = 2.5f;
        [Tooltip("Gift box / arrow colours for Common, Rare, Epic skill items.")]
        public Color[] RarityColors =
        {
            new Color(1f, 0.82f, 0.25f), new Color(0.3f, 0.64f, 1f), new Color(0.7f, 0.4f, 1f),
        };
        [Tooltip("Stat boxes use mint green (GDD 5).")]
        public Color StatItemColor = new Color(0.24f, 0.9f, 0.69f);
        public Color HealItemColor = new Color(1f, 0.42f, 0.54f);

        [Header("Skills (GDD 5-6)")]
        public int MaxActiveSkills = 5;
        public int MaxSkillLevel = 5;
        public float SkillWarnTime = 5f;

        [Header("Moment of release (GDD 10)")]
        public float ReleaseSlowMoDuration = 0.3f;
        public float ReleaseSlowMoScale = 0.25f;
        public float ReleaseZoom = 0.88f;
        public float ReleaseZoomDuration = 0.8f;

        [Header("Enemies (GDD 7, 12)")]
        public int MaxEnemies = 150;
        [Tooltip("Chasing speed never exceeds this fraction of the player's base speed.")]
        public float EnemySpeedCap = 1.05f;
        [Tooltip("Minimum spawn distance; the real camera half-diagonal is used when larger.")]
        public float SpawnMinDistance = 1100f;
        public float SpawnDistanceJitter = 250f;
        public float SpawnWallMargin = 150f;
        public float StuckDistance = 3000f;
        public float StuckTime = 10f;
        public float EliteHpMultiplier = 5f;
        public int EliteExpMultiplier = 5;
        public float EliteScale = 1.3f;
        public float EnemyObstacleLookAhead = 90f;

        [Header("Horde burst (GDD 7)")]
        public float HordeFirstTime = 60f;
        public float HordeInterval = 60f;
        public float HordeDurationMin = 3f;
        public float HordeDurationMax = 5f;
        public float HordeWarningTime = 1.5f;
        public float HordeSpawnRateMultiplier = 4f;

        [Header("Revive (GDD 11)")]
        [Range(0f, 1f)] public float ReviveHpFraction = 0.5f;
        public float ReviveInvulnerability = 3f;
        public float ReviveOfferSeconds = 10f;

        [Header("Coins (GDD 8)")]
        public float CoinsPerMinute = 20f;
        public int KillsPerCoin = 5;

        public float ToWorld(float gdu) => gdu * UnitsPerGduUnit;
        public float ToGdu(float world) => world / UnitsPerGduUnit;
    }
}
