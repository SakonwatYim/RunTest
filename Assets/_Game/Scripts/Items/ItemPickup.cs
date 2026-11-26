using UnityEngine;

namespace RunChildRun
{
    /// <summary>Gift box on the ground. Contents are decided when it spawns.</summary>
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] SpriteRenderer box;
        [Tooltip("Optional icon of the skill/stat inside, shown on top of the box.")]
        [SerializeField] SpriteRenderer icon;
        [Tooltip("Light beam shown on the guaranteed first item.")]
        [SerializeField] GameObject beam;
        [SerializeField] Transform visual;

        float bob;

        public ItemKind Kind { get; private set; }
        public Rarity Rarity { get; private set; }
        public SkillDef Skill { get; private set; }
        public StatKind Stat { get; private set; }
        public bool IsFirst { get; private set; }
        public float Remaining { get; set; }
        public bool Expires => !IsFirst;
        public Vector2 Position => transform.position;
        public Color Color { get; private set; }

        public void Setup(ItemKind kind, Rarity rarity, SkillDef skill, StatKind stat, bool isFirst, float lifetime,
            Color color, Sprite iconSprite)
        {
            Kind = kind;
            Rarity = rarity;
            Skill = skill;
            Stat = stat;
            IsFirst = isFirst;
            Remaining = lifetime;
            Color = color;
            bob = Random.value * 6f;
            if (box != null) box.color = color;
            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }
            if (beam != null) beam.SetActive(isFirst);
        }

        void Update()
        {
            bob += Time.deltaTime * 4f;
            if (visual != null) visual.localPosition = new Vector3(0f, Mathf.Sin(bob) * 0.08f, 0f);
            if (box != null && Expires && Remaining < 5f)
            {
                var c = Color;
                c.a = Mathf.Repeat(Remaining, 0.4f) < 0.2f ? 0.4f : 1f;
                box.color = c;
            }
        }
    }
}
