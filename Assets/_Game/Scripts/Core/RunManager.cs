
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RunChildRun
{
    /// <summary>Run state machine: Playing / LevelUp / Paused / Reviving / GameOver.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class RunManager : MonoBehaviour
    {
        public const string RunSceneName = "Run";
        public const string HubSceneName = "Hub";

        [SerializeField] RunContext ctx;

        readonly List<StatKind> choices = new List<StatKind>(3);
        bool firstShotPending;
        bool doubleCoinsUsed;
        int coinsEarned;
        float slowMoUntil = -1f;

        public RunState State { get; private set; }
        public StatBlock Stats { get; private set; }
        public MetaModifiers Meta { get; private set; }
        public RunPhase Phase { get; private set; } = RunPhase.Playing;
        public int CoinsSoFar => RunState.CoinsFor(State.Elapsed, State.Kills, ctx.Config.CoinsPerMinute, ctx.Config.KillsPerCoin);

        void Awake()
        {
            Time.timeScale = 1f;
            var save = SaveSystem.Data;
            Meta = MetaProgression.GetModifiers(save, ctx.Upgrades);
            State = new RunState { RerollsLeft = ctx.Config.RerollsPerRun };
            Stats = new StatBlock(ctx.StatDefs);
        }

        void Start()
        {
            ctx.Player.Init(ctx, Meta);
            ctx.PlayerHealth.Init(ctx, Meta);
            ctx.Enemies.Init(ctx);
            ctx.Spawner.Init(ctx);
            ctx.Projectiles.Init(ctx);
            ctx.Fx.Init(ctx);
            ctx.Skills.Init(ctx);
            ctx.Items.Init(ctx);
            ctx.CameraRig.Init(ctx);
            ctx.Audio.Init(SaveSystem.Data.Settings);
            ctx.Hud.Bind(ctx);
            ctx.LevelUp.Bind(this);
            ctx.Pause.Bind(this);
            ctx.Revive.Bind(this);
            ctx.GameOver.Bind(this);

            ctx.Enemies.EnemyKilled += OnEnemyKilled;
            ctx.PlayerHealth.Died += OnPlayerDied;
            ctx.PlayerHealth.Damaged += OnPlayerDamaged;
            ctx.Player.DashReady += () =>
            {
                ctx.Audio.Play(ctx.Sfx != null ? ctx.Sfx.DashReady : null);
                Haptics.Light();
            };
            ctx.Player.Dashed += () => ctx.Audio.Play(ctx.Sfx != null ? ctx.Sfx.Dash : null, 1f, 0.8f);

            ctx.Audio.StartMusic();
            SetPhase(RunPhase.Playing);
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (Phase != RunPhase.Playing) return;
            var dt = Time.deltaTime;
            State.Elapsed += dt;

            var rate = ExpCurve.TimeExpRate(ctx.Skills.AnyActive, ctx.Config.TimeExpUnarmed, ctx.Config.TimeExpArmed);
            State.AddExp(rate * dt * Meta.ExpGainMultiplier, ctx.Config);

            if (slowMoUntil > 0f && Time.unscaledTime >= slowMoUntil)
            {
                slowMoUntil = -1f;
                Time.timeScale = 1f;
            }

            if (State.PendingLevelUps > 0) OpenLevelUp();
        }

        void OnEnemyKilled(Enemy e, bool rewarded)
        {
            ctx.Fx.Burst(e.Position, new Color(1f, 0.6f, 0.35f));
            if (!rewarded) return;
            State.Kills++;
            State.AddExp(e.ExpValue * Meta.ExpGainMultiplier, ctx.Config);
            ctx.Fx.ExpSpark(e.Position);
            ctx.Audio.PlayExpTick();
        }

        void OnPlayerDamaged(float amount)
        {
            ctx.CameraRig.Shake(0.1f, 0.15f);
            ctx.Audio.Play(ctx.Sfx != null ? ctx.Sfx.PlayerHurt : null);
            Haptics.Light();
        }

        // ---------- First item: "moment of release" (GDD 10) ----------

        public void OnFirstItemCollected()
        {
            State.FirstItemCollected = true;
            firstShotPending = true;
            Time.timeScale = ctx.Config.ReleaseSlowMoScale;
            slowMoUntil = Time.unscaledTime + ctx.Config.ReleaseSlowMoDuration;
            ctx.CameraRig.ZoomPunch(ctx.Config.ReleaseZoom, ctx.Config.ReleaseZoomDuration);
            ctx.Audio.SetActionLayer(true);
        }

        /// <summary>True exactly once after the first item: that shot gets a bigger effect.</summary>
        public bool ConsumeFirstShot()
        {
            if (!firstShotPending) return false;
            firstShotPending = false;
            return true;
        }

        // ---------- Level up (GDD 4) ----------

        void OpenLevelUp()
        {
            Stats.PickChoices(ctx.Config.LevelUpChoices, () => Random.value, choices);
            if (choices.Count == 0)
            {
                State.PendingLevelUps = 0;
                return;
            }
            SetPhase(RunPhase.LevelUp);
            ctx.LevelUp.Show(choices, State.RerollsLeft, ctx.Config.LevelUpSelectLock);
            ctx.Audio.Play(ctx.Sfx != null ? ctx.Sfx.LevelUp : null);
            Haptics.Light();
        }

        public void RerollLevelUp()
        {
            if (Phase != RunPhase.LevelUp || State.RerollsLeft <= 0) return;
            State.RerollsLeft--;
            Stats.PickChoices(ctx.Config.LevelUpChoices, () => Random.value, choices);
            ctx.LevelUp.Show(choices, State.RerollsLeft, 0f);
        }

        public void SelectStat(StatKind kind)
        {
            if (Phase != RunPhase.LevelUp) return;
            if (Stats.LevelUp(kind)) ApplyStatChange(kind);
            State.PendingLevelUps = Mathf.Max(0, State.PendingLevelUps - 1);
            if (State.PendingLevelUps > 0)
            {
                OpenLevelUp();
                return;
            }
            ctx.LevelUp.Hide();
            SetPhase(RunPhase.Playing);
            ctx.PlayerHealth.GrantInvulnerability(ctx.Config.LevelUpIFrames);
        }

        public void ApplyStatChange(StatKind kind)
        {
            if (kind == StatKind.MaxHp) ctx.PlayerHealth.RefreshMaxHp(Stats.BonusMaxHp);
        }

        // ---------- Pause ----------

        public void Pause()
        {
            if (Phase != RunPhase.Playing) return;
            SetPhase(RunPhase.Paused);
            ctx.Pause.Show();
        }

        public void Resume()
        {
            if (Phase != RunPhase.Paused) return;
            ctx.Pause.Hide();
            SetPhase(RunPhase.Playing);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Pause();
        }

        void OnApplicationFocus(bool focus)
        {
#if !UNITY_EDITOR
            if (!focus) Pause();
#endif
        }

        // ---------- Death / revive / game over ----------

        void OnPlayerDied()
        {
            ctx.Audio.Play(ctx.Sfx != null ? ctx.Sfx.PlayerDie : null);
            if (!State.Revived && Ads.Rewarded.IsReady(AdPlacements.Revive))
            {
                SetPhase(RunPhase.Reviving);
                ctx.Revive.Show(ctx.Config.ReviveOfferSeconds);
            }
            else
            {
                EndRun();
            }
        }

        public void WatchReviveAd()
        {
            if (Phase != RunPhase.Reviving) return;
            ctx.Revive.SetBusy(true);
            Ads.Rewarded.Show(AdPlacements.Revive, ok =>
            {
                if (this == null) return;
                ctx.Revive.SetBusy(false);
                if (ok) DoRevive();
                else EndRun();
            });
        }

        void DoRevive()
        {
            State.Revived = true;
            ctx.Revive.Hide();
            ctx.Projectiles.ClearEnemyShots();
            ctx.PlayerHealth.Revive(ctx.Config.ReviveHpFraction, ctx.Config.ReviveInvulnerability);
            SetPhase(RunPhase.Playing);
        }

        public void DeclineRevive()
        {
            if (Phase != RunPhase.Reviving) return;
            ctx.Revive.Hide();
            EndRun();
        }

        void EndRun()
        {
            SetPhase(RunPhase.GameOver);
            ctx.Audio.SetActionLayer(false);
            coinsEarned = CoinsSoFar;
            var save = SaveSystem.Data;
            var previousBest = save.BestTime;
            save.Coins += coinsEarned;
            save.TotalRuns++;
            save.BestTime = Mathf.Max(save.BestTime, State.Elapsed);
            save.BestLevel = Mathf.Max(save.BestLevel, State.Level);
            save.BestKills = Mathf.Max(save.BestKills, State.Kills);
            SaveSystem.Save();
            ctx.GameOver.Show(State.Elapsed, previousBest, State.Elapsed > previousBest, coinsEarned, State.Kills, State.Level);
        }

        public void WatchDoubleCoinsAd()
        {
            if (Phase != RunPhase.GameOver || doubleCoinsUsed) return;
            doubleCoinsUsed = true;
            ctx.GameOver.SetDoubleCoinsBusy();
            Ads.Rewarded.Show(AdPlacements.DoubleCoins, ok =>
            {
                if (this == null || !ok) return;
                SaveSystem.Data.Coins += coinsEarned;
                SaveSystem.Save();
                coinsEarned *= 2;
                ctx.GameOver.SetCoins(coinsEarned, true);
            });
        }

        public void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(RunSceneName);
        }

        public void GoToHub()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(HubSceneName);
        }

        void SetPhase(RunPhase phase)
        {
            Phase = phase;
            if (phase == RunPhase.Playing) Time.timeScale = slowMoUntil > 0f ? ctx.Config.ReleaseSlowMoScale : 1f;
            else Time.timeScale = 0f;
        }

    }
}
