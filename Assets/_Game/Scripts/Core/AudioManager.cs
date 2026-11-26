using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Small pool of one-shot sources plus two music layers (GDD 10): the tense layer plays from
    /// the start, the driving layer is started in sync but muted and fades in after the first item.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] SfxBank bank;
        [SerializeField] AudioSource[] sfxSources;
        [SerializeField] AudioSource musicTense;
        [SerializeField] AudioSource musicAction;
        [Tooltip("Seconds after the last kill before the combo pitch resets.")]
        [SerializeField] float comboWindow = 0.6f;
        [Tooltip("Minimum real seconds between enemy-hit sounds (keeps big fights from getting noisy).")]
        [SerializeField] float hitInterval = 0.06f;
        [SerializeField] float musicFadeSeconds = 1.2f;

        int next;
        float sfxVolume = 1f;
        float musicVolume = 0.7f;
        int combo;
        float lastTick = -10f;
        float lastHit = -10f;
        float actionTarget;
        float actionLevel;

        public void Init(SettingsData settings)
        {
            sfxVolume = settings.SfxVolume;
            musicVolume = settings.MusicVolume;
        }

        public void Play(AudioClip clip, float pitch = 1f, float volume = 1f)
        {
            if (clip == null || sfxSources == null || sfxSources.Length == 0 || sfxVolume <= 0f) return;
            var s = sfxSources[next];
            next = (next + 1) % sfxSources.Length;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume * sfxVolume);
        }

        public void PlayHit()
        {
            if (bank == null || Time.unscaledTime - lastHit < hitInterval) return;
            lastHit = Time.unscaledTime;
            Play(bank.EnemyHit, 0.9f + Random.value * 0.25f, 0.35f);
        }

        public void PlayExpTick()
        {
            if (bank == null || bank.ExpTick == null) return;
            combo = Time.unscaledTime - lastTick < comboWindow ? Mathf.Min(combo + 1, 12) : 0;
            lastTick = Time.unscaledTime;
            Play(bank.ExpTick, 1f + combo * 0.06f, 0.45f);
        }

        /// <summary>Starts both layers on the same DSP time so they stay in sync; the action layer starts silent.</summary>
        public void StartMusic()
        {
            if (bank == null) return;
            var start = AudioSettings.dspTime + 0.1;
            Prepare(musicTense, bank.MusicTense, musicVolume);
            Prepare(musicAction, bank.MusicAction, 0f);
            if (musicTense != null && musicTense.clip != null) musicTense.PlayScheduled(start);
            if (musicAction != null && musicAction.clip != null) musicAction.PlayScheduled(start);
            actionLevel = 0f;
            actionTarget = 0f;
        }

        /// <summary>Fades the driving layer in (first item) or out (game over).</summary>
        public void SetActionLayer(bool on) => actionTarget = on ? 1f : 0f;

        void Update()
        {
            if (musicAction == null || Mathf.Approximately(actionLevel, actionTarget)) return;
            actionLevel = Mathf.MoveTowards(actionLevel, actionTarget, Time.unscaledDeltaTime / Mathf.Max(0.01f, musicFadeSeconds));
            musicAction.volume = actionLevel * musicVolume;
        }

        static void Prepare(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null) return;
            source.Stop();
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
        }
    }
}
