using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Scene-level wiring for the Run scene: data assets and system references, assigned in the
    /// Inspector. Systems reach each other through <see cref="Current"/>.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class RunContext : MonoBehaviour
    {
        public static RunContext Current { get; private set; }

        [Header("Data")]
        public GameConfig Config;
        public WaveTable Waves;
        public StatDef[] StatDefs;
        public SkillDef[] SkillDefs;
        public MetaUpgradeDef[] Upgrades;
        public SfxBank Sfx;

        [Header("Systems")]
        public RunManager Run;
        public PlayerController Player;
        public PlayerHealth PlayerHealth;
        public InputReader Input;
        public ArenaView Arena;
        public EnemyManager Enemies;
        public EnemySpawner Spawner;
        public ProjectileSystem Projectiles;
        public FxSystem Fx;
        public SkillSystem Skills;
        public ItemDropSystem Items;
        public CameraRig CameraRig;
        public AudioManager Audio;

        [Header("UI")]
        public HudView Hud;
        public LevelUpView LevelUp;
        public PauseView Pause;
        public ReviveView Revive;
        public GameOverView GameOver;

        public float W(float gdu) => Config.ToWorld(gdu);

        void Awake()
        {
            Current = this;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
