using UnityEngine;
using Core.Interfaces;
using Core;

namespace Managers
{
    /// <summary>
    /// Singleton global de gestion configuracion (Settings) cua game: music/SFX volume si UseSkillKey.
    /// Lua gia tri trong PlayerPrefs de khoi phuc sau restart game.
    /// Dia search vao assembly Managers (implement ISettingsManager trong Core) de giu
    /// Core khong phu thuoc de no, va cac assembly khac chi dung interface (qua SettingsService).
    /// </summary>
    public class SettingsManager : MonoBehaviour, ISettingsManager
    {
        #region Constants

        // PlayerPrefs key dung de luu gia tri.
        private const string PREF_MUSIC_VOLUME = "settings.musicVolume";
        private const string PREF_SFX_VOLUME = "settings.sfxVolume";
        private const string PREF_USE_SKILL_KEY = "settings.useSkillKey";
        private const string PREF_SCREEN_SHAKE_ENABLED = "settings.screenShakeEnabled";

        /// <summary>Gia tri mac dinh.</summary>
        private const float DefaultVolume = 50f;
        private const string DefaultUseSkillKey = "E";
        private const bool DefaultScreenShakeEnabled = true;

        /// <summary>Media choice constraint.</summary>
        private const float MinVolume = 0f;
        private const float MaxVolume = 100f;

        #endregion

        #region Singleton

        /// <summary>
        /// Instance singleton cua SettingsManager.
        /// </summary>
        public static SettingsManager Instance { get; private set; }

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Khoi tao singleton va dang ky SettingsService.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SettingsService.Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region ISettingsManager

        /// <inheritdoc/>
        public float MusicVolume => PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, DefaultVolume);

        /// <inheritdoc/>
        public void SetMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, Mathf.Clamp(value, MinVolume, MaxVolume));
            PlayerPrefs.Save();

            // Broadcast de cac listeners (vi do BGMController) ap dung coi nay live.
            GameEvents.TriggerSettingsMusicVolumeChanged(MusicVolume);
        }

        /// <inheritdoc/>
        public float SfxVolume => PlayerPrefs.GetFloat(PREF_SFX_VOLUME, DefaultVolume);

        /// <inheritdoc/>
        public void SetSfxVolume(float value)
        {
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, Mathf.Clamp(value, MinVolume, MaxVolume));
            PlayerPrefs.Save();

            // Broadcast de cac listeners (SFX system) ap dung sfx volume live.
            GameEvents.TriggerSettingsSfxVolumeChanged(SfxVolume);
        }

        /// <inheritdoc/>
        public string UseSkillKey => PlayerPrefs.GetString(PREF_USE_SKILL_KEY, DefaultUseSkillKey);

        /// <inheritdoc/>
        public void SetUseSkillKey(string keyName)
        {
            if (keyName == null || keyName.Length == 0)
            {
                return;
            }

            PlayerPrefs.SetString(PREF_USE_SKILL_KEY, keyName);
            PlayerPrefs.Save();

            // Broadcast de cac listeners (vi du HUDManager) cap nhat UI live.
            GameEvents.TriggerSettingsUseSkillKeyChanged(keyName);
        }

        /// <inheritdoc/>
        public bool ScreenShakeEnabled => PlayerPrefs.GetInt(PREF_SCREEN_SHAKE_ENABLED, DefaultScreenShakeEnabled ? 1 : 0) == 1;

        /// <inheritdoc/>
        public void SetScreenShakeEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(PREF_SCREEN_SHAKE_ENABLED, enabled ? 1 : 0);
            PlayerPrefs.Save();

            // Broadcast de cac listeners (CameraController, ScreenOverlayEffect) ap dung live.
            GameEvents.TriggerSettingsScreenShakeChanged(enabled);
        }

        #endregion
    }
}