using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Small HP bar that travels with the player (world-space canvas on the Player prefab).</summary>
    public sealed class WorldHpBar : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;
        [SerializeField] Image fill;
        [SerializeField] TMP_Text label;

        int shownHp = -1;
        int shownMax = -1;

        void OnEnable()
        {
            if (health != null) health.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (health != null) health.Changed -= Refresh;
        }

        void Refresh()
        {
            if (health == null) return;
            if (fill != null) fill.fillAmount = health.Hp01;
            var hp = Mathf.CeilToInt(health.Hp);
            var max = Mathf.CeilToInt(health.MaxHp);
            if (hp == shownHp && max == shownMax) return;
            shownHp = hp;
            shownMax = max;
            if (label != null) label.text = hp + "/" + max;
        }
    }
}
