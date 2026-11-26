using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>In-run HUD (Figma "Gameplay"): coins, kills, timer, level + EXP bar, pause, skill slots, joystick, Dash.</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text coinText;
        [SerializeField] TMP_Text killText;
        [SerializeField] TMP_Text timerText;
        [SerializeField] TMP_Text levelText;
        [Tooltip("Horizontal filled image.")]
        [SerializeField] Image expFill;
        [SerializeField] Button pauseButton;
        [SerializeField] SkillSlotView[] skillSlots;
        [SerializeField] DashButtonView dashButton;
        [SerializeField] FloatingJoystick joystick;
        [SerializeField] ItemIndicatorLayer itemIndicators;
        [SerializeField] HordeWarningView hordeWarning;
        [SerializeField] ToastView toast;
        [SerializeField] string itemSpawnedMessage = "Gift box appeared! Follow the arrow!";

        RunContext ctx;
        int shownCoins = -1;
        int shownKills = -1;
        int shownSeconds = -1;
        int shownLevel = -1;

        public void Bind(RunContext context)
        {
            ctx = context;
            pauseButton.onClick.AddListener(ctx.Run.Pause);
            dashButton.Bind(ctx);
            joystick.ApplySettings(SaveSystem.Data.Settings);
            itemIndicators.Bind(ctx);
            ctx.Items.ItemSpawned += _ =>
            {
                ShowToast(itemSpawnedMessage, new Color(1f, 0.85f, 0.25f));
                ctx.Audio.Play(ctx.Sfx != null ? ctx.Sfx.ItemSpawned : null);
            };
            ctx.Spawner.HordeWarning += hordeWarning.Show;
        }

        public void ShowToast(string message, Color color) => toast.Show(message, color);

        void Update()
        {
            if (ctx == null) return;
            var run = ctx.Run;
            var state = run.State;

            var coins = run.CoinsSoFar;
            if (coins != shownCoins)
            {
                shownCoins = coins;
                coinText.text = coins.ToString("N0");
            }
            if (state.Kills != shownKills)
            {
                shownKills = state.Kills;
                killText.text = state.Kills.ToString("0000");
            }
            var seconds = Mathf.FloorToInt(state.Elapsed);
            if (seconds != shownSeconds)
            {
                shownSeconds = seconds;
                timerText.text = RunState.FormatTime(seconds);
            }
            if (state.Level != shownLevel)
            {
                shownLevel = state.Level;
                levelText.text = "Level:" + state.Level;
            }
            expFill.fillAmount = state.ExpProgress01(ctx.Config);

            var active = ctx.Skills.Inventory.Active;
            for (var i = 0; i < skillSlots.Length; i++)
            {
                skillSlots[i].Set(i < active.Count ? active[i] : null, ctx.Config.SkillWarnTime);
            }
        }
    }
}
