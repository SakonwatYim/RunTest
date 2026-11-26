using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Level-up card (Figma "Level Up!"): title, icon, short effect, level / max.</summary>
    public sealed class StatCardView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TMP_Text title;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text description;
        [SerializeField] TMP_Text progress;

        StatKind kind;
        Action<StatKind> onPick;

        public Button Button => button;

        void Awake() => button.onClick.AddListener(() => onPick?.Invoke(kind));

        public void Set(StatDef def, int level, Action<StatKind> pick)
        {
            kind = def.Kind;
            onPick = pick;
            title.text = def.DisplayName;
            description.text = def.Describe();
            progress.text = level + "/" + def.MaxLevel;
            if (icon != null)
            {
                icon.sprite = def.Icon;
                icon.enabled = def.Icon != null;
            }
        }
    }
}
