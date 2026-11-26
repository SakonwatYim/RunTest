using UnityEngine;

namespace RunChildRun
{
    /// <summary>Looping Hub music that follows the Music volume slider live.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuMusic : MonoBehaviour
    {
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip clip;
        [Range(0f, 1f)] [SerializeField] float volume = 0.8f;

        void Start()
        {
            if (source == null) source = GetComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = volume * SaveSystem.Data.Settings.MusicVolume;
            if (clip != null) source.Play();
        }

        void Update()
        {
            source.volume = volume * SaveSystem.Data.Settings.MusicVolume;
        }
    }
}
