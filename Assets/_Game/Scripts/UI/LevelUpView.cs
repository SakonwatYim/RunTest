using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Pick 1 of 3 stat cards; 1 reroll per run; buttons locked ~1 s against stray taps (GDD 4).</summary>
    public sealed class LevelUpView : MonoBehaviour
    {
        [SerializeField] StatCardView[] cards;
        [SerializeField] Button rerollButton;
        [SerializeField] TMP_Text rerollLabel;
        [SerializeField] CanvasGroup cardsGroup;

        RunManager run;
        float unlockAt;

        public void Bind(RunManager manager)
        {
            run = manager;
            rerollButton.onClick.AddListener(run.RerollLevelUp);
        }

        public void Show(List<StatKind> choices, int rerollsLeft, float lockSeconds)
        {
            gameObject.SetActive(true);
            for (var i = 0; i < cards.Length; i++)
            {
                var on = i < choices.Count;
                cards[i].gameObject.SetActive(on);
                if (!on) continue;
                var def = run.Stats.GetDef(choices[i]);
                cards[i].Set(def, run.Stats.GetLevel(choices[i]), run.SelectStat);
            }
            rerollButton.gameObject.SetActive(rerollsLeft > 0);
            if (rerollLabel != null) rerollLabel.text = "Reroll (" + rerollsLeft + ")";
            unlockAt = Time.unscaledTime + lockSeconds;
            SetInteractable(lockSeconds <= 0f);
        }

        public void Hide() => gameObject.SetActive(false);

        void Update()
        {
            if (Time.unscaledTime >= unlockAt && cardsGroup != null && !cardsGroup.interactable) SetInteractable(true);
        }

        void SetInteractable(bool on)
        {
            if (cardsGroup != null)
            {
                cardsGroup.interactable = on;
                cardsGroup.alpha = on ? 1f : 0.75f;
            }
            rerollButton.interactable = on;
        }
    }
}
