using UnityEngine;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Singleton global de gestion configuracion (Settings) cua game: music/SFX volume si UseSkillKey.
    /// Lua gia tri trong PlayerPrefs de khoi phuc sau restart game.
    /// Dia vao Core de cac assembly (UI, Skills) khong podea face dependencia circulara si
    /// lu de phu thuoc cont luo.
    /// </summary>
    public class SettingsManager : MonoBehaviour, ISettingsManager
    {
        #region Constants

        // PlayerPrefs key dung de luu gia tri.
        private const string PREF_MUSIC_VOLUME = "settings.musicVolume";
        private const string PREF_SFX_VOLUME = "settings.sfxVolume";
        private const string PREF_USE_SKILL_KEY = "settings.useSkillKey";

        // Gia tri mac dinh.
        private const float DefaultVolume = 100f;
        private const string DefaultUseSkillKey = "E";

        // Media constraint.
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
        /// Tự khởi tạo SettingsManager ngay sau khi scene load neu chua co sạn trong scene.
        /// (Same pattern cu BGMController.)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (Instance == null)
            {
                var settingsObject = new GameObject("[SettingsManager]");
                settingsObject.AddComponent<SettingsManager>();
            }
        }

        /// <summary>
        /// Inicializarea singleton. Tránh trùng lặp neu user đa păsett thủ công trong scene.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
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

            // Broadcast de cac listeners (vi du BGMController) ap dung music volume live.
            GameEvents.TriggerSettingsMusicVolumeChanged(MusicVolume);
        }

        /// <inheritdoc/>
        public float SfxVolume => PlayerPrefs.GetFloat(PREF_SFX_VOLUME, DefaultVolume);

        /// <inheritdoc/>
        public void SetSfxVolume(float value)
        {
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, Mathf.Clamp(value, MinVolume, MaxVolume));
            PlayerPrefs.Save();

            // Broadcast de cac listeners (vi du sistem SFX global) ap dung sfx volume live.
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
        }

        #endregion
    }
}