using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Plays a click when the button is pressed. Put it on any button prefab.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonClickSound : MonoBehaviour
    {
        [SerializeField] AudioClip clip;
        [Range(0f, 1f)] [SerializeField] float volume = 0.7f;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => UiAudio.Play(clip, volume));
        }
    }
}
