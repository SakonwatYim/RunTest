using System;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Settings (GDD 3, 10): joystick mode/size/opacity, screen shake, haptics, volumes.</summary>
    public sealed class SettingsView : MonoBehaviour
    {
        [SerializeField] Button backButton;
        [SerializeField] Toggle fixedJoystickToggle;
        [SerializeField] Slider joystickSizeSlider;
        [SerializeField] Slider joystickOpacitySlider;
        [SerializeField] Toggle screenShakeToggle;
        [SerializeField] Toggle hapticsToggle;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;

        bool loading;

        public event Action Closed;

        public void Bind()
        {
            backButton.onClick.AddListener(Hide);
            fixedJoystickToggle.onValueChanged.AddListener(_ => Store());
            joystickSizeSlider.onValueChanged.AddListener(_ => Store());
            joystickOpacitySlider.onValueChanged.AddListener(_ => Store());
            screenShakeToggle.onValueChanged.AddListener(_ => Store());
            hapticsToggle.onValueChanged.AddListener(_ => Store());
            musicSlider.onValueChanged.AddListener(_ => Store());
            sfxSlider.onValueChanged.AddListener(_ => Store());
        }

        public void Show()
        {
            gameObject.SetActive(true);
            loading = true;
            var s = SaveSystem.Data.Settings;
            fixedJoystickToggle.isOn = s.FixedJoystick;
            joystickSizeSlider.value = s.JoystickScale;
            joystickOpacitySlider.value = s.JoystickOpacity;
            screenShakeToggle.isOn = s.ScreenShake;
            hapticsToggle.isOn = s.Haptics;
            musicSlider.value = s.MusicVolume;
            sfxSlider.value = s.SfxVolume;
            loading = false;
        }

        public void Hide()
        {
            SaveSystem.Save();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        void Store()
        {
            if (loading) return;
            var s = SaveSystem.Data.Settings;
            s.FixedJoystick = fixedJoystickToggle.isOn;
            s.JoystickScale = joystickSizeSlider.value;
            s.JoystickOpacity = joystickOpacitySlider.value;
            s.ScreenShake = screenShakeToggle.isOn;
            s.Haptics = hapticsToggle.isOn;
            s.MusicVolume = musicSlider.value;
            s.SfxVolume = sfxSlider.value;
        }
    }
}
