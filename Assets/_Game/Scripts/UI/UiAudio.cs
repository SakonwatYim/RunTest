using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// UI sounds through one persistent source, so a click still finishes when the button loads
    /// another scene. Follows the SFX volume setting.
    /// </summary>
    public static class UiAudio
    {
        static AudioSource source;

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            if (source == null)
            {
                var go = new GameObject("[UiAudio]") { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.ignoreListenerPause = true;
            }
            source.PlayOneShot(clip, volume * SaveSystem.Data.Settings.SfxVolume);
        }
    }
}
