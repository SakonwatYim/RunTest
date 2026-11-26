using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>One of the 5 active-skill slots: icon, level and a countdown ring that blinks near the end.</summary>
    public sealed class SkillSlotView : MonoBehaviour
    {
        [Tooltip("Shown only while a skill occupies the slot.")]
        [SerializeField] GameObject content;
        [SerializeField] Image icon;
        [Tooltip("Radial-filled ring: fill = time left.")]
        [SerializeField] Image timerRing;
        [SerializeField] TMP_Text levelText;
        [SerializeField] CanvasGroup blinkGroup;

        ActiveSkill shown;
        int shownLevel = -1;

        public void Set(ActiveSkill skill, float warnTime)
        {
            if (skill == null)
            {
                if (content.activeSelf) content.SetActive(false);
                shown = null;
                shownLevel = -1;
                return;
            }

            if (skill != shown)
            {
                shown = skill;
                content.SetActive(true);
                if (icon != null)
                {
                    icon.sprite = skill.Def.Icon;
                    icon.color = skill.Def.Icon != null ? Color.white : skill.Def.Color;
                }
                shownLevel = -1;
            }
            if (skill.Level != shownLevel)
            {
                shownLevel = skill.Level;
                if (levelText != null) levelText.text = "Lv" + skill.Level;
            }
            if (timerRing != null) timerRing.fillAmount = skill.Remaining01;
            if (blinkGroup != null)
            {
                blinkGroup.alpha = skill.RemainingTime < warnTime && Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f ? 0.35f : 1f;
            }
        }
    }
}
