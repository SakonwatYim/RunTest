using UnityEngine;

namespace RunChildRun
{
    /// <summary>Sound slots. Empty slots are silent, so audio can be added later without code changes.</summary>
    [CreateAssetMenu(menuName = "Run Child Run/Sfx Bank", fileName = "SfxBank")]
    public sealed class SfxBank : ScriptableObject
    {
        [Header("Music layers (GDD 10)")]
        public AudioClip MusicTense;
        [Tooltip("Layer added after the first item.")]
        public AudioClip MusicAction;
        [Tooltip("Hub menu loop.")]
        public AudioClip MusicMenu;

        [Header("Gameplay")]
        public AudioClip Shoot;
        public AudioClip Shockwave;
        public AudioClip EnemyHit;
        [Tooltip("Kill-combo tick; pitch rises with the combo.")]
        public AudioClip ExpTick;
        public AudioClip Pickup;
        [Tooltip("Distinct chord for the first item (moment of release).")]
        public AudioClip FirstPickup;
        public AudioClip LevelUp;
        public AudioClip Dash;
        public AudioClip DashReady;
        public AudioClip PlayerHurt;
        public AudioClip PlayerDie;
        public AudioClip HordeWarning;
        public AudioClip ItemSpawned;

        [Header("UI")]
        public AudioClip ButtonClick;
    }
}
