using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using Core;
using Core.Interfaces;
using System;

namespace UI
{
    /// <summary>
    /// Panel de setari reusable (Settings): slider Music, slider SFX si rebind UseSkillKey.
    /// Citete giarile din singleton SettingsManager (Core) de restaurarea candse specta game.
    /// Poate fi folosit in orice panel/popup (SettingsPopup, PauseMenu, MainUI).
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        #region Fields

        [Header("Volume")]
        [Tooltip("Slider Music volume (0 - 100).")]
        [SerializeField] private Slider _musicSlider;
        [Tooltip("Slider SFX volume (0 - 100).")]
        [SerializeField] private Slider _sfxSlider;

        [Header("Use Skill Key")]
        [Tooltip("Botton pentru rebind key-ului de skill. Cand anc apasai, textul se schimba in 'Press a Key...'.")]
        [SerializeField] private Button _useSkillKeyButton;
        [Tooltip("(Optional) Label text de pe botton. Neu nu este la, se accune dupa child Text.")]
        [SerializeField] private TextMeshProUGUI _useSkillKeyLabel;

        [Header("Screen Shake & Overlay")]
        [Tooltip("Toggle don hoac toggle bat hieu ung lac man hinh.")]
        [SerializeField] private Toggle _screenShakeToggle;
        [Tooltip("Toggle 'On' trong ToggleGroup (neu dung 2 toggle bat/tat).")]
        [SerializeField] private Toggle _screenShakeOnToggle;
        [Tooltip("Toggle 'Off' trong ToggleGroup (neu dung 2 toggle bat/tat).")]
        [SerializeField] private Toggle _screenShakeOffToggle;
        [Tooltip("(Tuy chon) ToggleGroup cho cac toggle Screen Shake.")]
        [SerializeField] private ToggleGroup _screenShakeToggleGroup;

        // Starea cand asteptam o key de rebind.
        private bool _isWaitingForKey;

        // Guard pentru sincronizarea valorilor aplicate din Refresh().
        private bool _isApplyingState;

        // Set de key speciale (nevalid pentru rebind), comun pentru tut instancele.
        private static readonly HashSet<Key> _specialKeys = new HashSet<Key>();

        // Textul sau afisat cand rebind este activ.
        private static readonly string WaitingForKeyLabel = "Press a Key...";

        #endregion

        #region Properties

        /// <summary>
        /// Returneaza true cand asteptam o key.
        /// </summary>
        public bool IsWaitingForKey => _isWaitingForKey;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupSpecialKeys();

            // Dupa nu este labelul, accuna din child Text al bottonului.
            if (_useSkillKeyLabel == null && _useSkillKeyButton != null)
            {
                _useSkillKeyLabel = _useSkillKeyButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.AddListener(HandleMusicSliderChanged);
            }
            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.AddListener(HandleSfxSliderChanged);
            }
            if (_useSkillKeyButton != null)
            {
                _useSkillKeyButton.onClick.AddListener(OnUseSkillKeyClicked);
            }
            if (_screenShakeToggle != null)
            {
                _screenShakeToggle.onValueChanged.AddListener(HandleScreenShakeToggleChanged);
            }
            if (_screenShakeOnToggle != null && _screenShakeOffToggle != null)
            {
                if (_screenShakeToggleGroup != null)
                {
                    _screenShakeOnToggle.group = _screenShakeToggleGroup;
                    _screenShakeOffToggle.group = _screenShakeToggleGroup;
                }

                _screenShakeOnToggle.onValueChanged.AddListener(HandleScreenShakeGroupToggleChanged);
                _screenShakeOffToggle.onValueChanged.AddListener(HandleScreenShakeGroupToggleChanged);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.RemoveListener(HandleMusicSliderChanged);
            }
            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.RemoveListener(HandleSfxSliderChanged);
            }
            if (_useSkillKeyButton != null)
            {
                _useSkillKeyButton.onClick.RemoveListener(OnUseSkillKeyClicked);
            }
            if (_screenShakeToggle != null)
            {
                _screenShakeToggle.onValueChanged.RemoveListener(HandleScreenShakeToggleChanged);
            }
            if (_screenShakeOnToggle != null && _screenShakeOffToggle != null)
            {
                _screenShakeOnToggle.onValueChanged.RemoveListener(HandleScreenShakeGroupToggleChanged);
                _screenShakeOffToggle.onValueChanged.RemoveListener(HandleScreenShakeGroupToggleChanged);
            }
        }

        /// <summary>
        /// In Update: cand asteptam o key, verificam care key a fost apasat prin
        /// InputSystem Keyboard.current pentru rebind.
        /// </summary>
        private void Update()
        {
            if (!_isWaitingForKey)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // Tim phim duoc nhan tren frame nay. Bo qua Key.None (khong hop le).
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (key == Key.None) continue;

                var keyControl = keyboard[key];
                if (keyControl != null && keyControl.wasPressedThisFrame)
                {
                    HandlePressedKey(key);
                    if (!_isWaitingForKey)
                    {
                        return;
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Umple UI-ul cum valorile din SettingsManager (apelat cand dechidere o panel).
        /// </summary>
        public void Refresh()
        {
            var settings = SettingsService.Instance;

            _isApplyingState = true;
            if (_musicSlider != null && settings != null)
            {
                _musicSlider.value = settings.MusicVolume;
            }
            if (_sfxSlider != null && settings != null)
            {
                _sfxSlider.value = settings.SfxVolume;
            }

            if (settings != null)
            {
                bool shakeEnabled = settings.ScreenShakeEnabled;
                if (_screenShakeToggle != null)
                {
                    _screenShakeToggle.isOn = shakeEnabled;
                }
                if (_screenShakeOnToggle != null && _screenShakeOffToggle != null)
                {
                    _screenShakeOnToggle.isOn = shakeEnabled;
                    _screenShakeOffToggle.isOn = !shakeEnabled;
                }
            }
            _isApplyingState = false;

            RefreshUseSkillKeyLabel();
        }

        /// <summary>
        /// Anula rebind-ul cand este in curs (cand inchide o panel) si reseteza textul bottonului.
        /// </summary>
        public void CancelRebind()
        {
            if (!_isWaitingForKey)
            {
                return;
            }

            _isWaitingForKey = false;
            RefreshUseSkillKeyLabel();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Construieste lista de key speciale/modifier/system (khong poate fi reboot pentru skill).
        /// Escape este tratat separat pentru cancel rebind.
        /// </summary>
        private static void SetupSpecialKeys()
        {
            if (_specialKeys.Count > 0)
            {
                return;
            }

            // Key speciale suoarbo ignorate cand rebind.
            _specialKeys.Add(Key.LeftShift);
            _specialKeys.Add(Key.RightShift);
            _specialKeys.Add(Key.LeftCtrl);
            _specialKeys.Add(Key.RightCtrl);
            _specialKeys.Add(Key.LeftAlt);
            _specialKeys.Add(Key.RightAlt);
            _specialKeys.Add(Key.LeftMeta);
            _specialKeys.Add(Key.RightMeta);
            _specialKeys.Add(Key.LeftWindows);
            _specialKeys.Add(Key.RightWindows);
            _specialKeys.Add(Key.LeftCommand);
            _specialKeys.Add(Key.RightCommand);
            _specialKeys.Add(Key.LeftApple);
            _specialKeys.Add(Key.RightApple);
            _specialKeys.Add(Key.ContextMenu);

            // Enter/Tab/Backspace.
            _specialKeys.Add(Key.Enter);
            _specialKeys.Add(Key.Tab);
            _specialKeys.Add(Key.Backspace);

            // Navigation/lock/system.
            _specialKeys.Add(Key.UpArrow);
            _specialKeys.Add(Key.DownArrow);
            _specialKeys.Add(Key.LeftArrow);
            _specialKeys.Add(Key.RightArrow);
            _specialKeys.Add(Key.Home);
            _specialKeys.Add(Key.End);
            _specialKeys.Add(Key.PageUp);
            _specialKeys.Add(Key.PageDown);
            _specialKeys.Add(Key.Insert);
            _specialKeys.Add(Key.Delete);
            _specialKeys.Add(Key.CapsLock);
            _specialKeys.Add(Key.NumLock);
            _specialKeys.Add(Key.ScrollLock);
            _specialKeys.Add(Key.PrintScreen);
            _specialKeys.Add(Key.Pause);

            // Function keys F1-F24.
            for (int i = 1; i <= 24; ++i)
            {
                // Enum.TryParse tra ve false daca key-ul nu existe in enum.
                if (Enum.TryParse("F" + i, true, out Key functionKey))
                {
                    _specialKeys.Add(functionKey);
                }
            }
        }

        /// <summary>
        /// Xul ama key-ul apasat cand este rebind: Escape anula, key speciale suorbo ignorate,
        /// iar restul este setat cum skill key.
        /// </summary>
        /// <param name="key">Key-ul apasat.</param>
        private void HandlePressedKey(Key key)
        {
            if (key == Key.Escape)
            {
                // Escape anula rebind-ul.
                CancelRebind();
                return;
            }

            if (_specialKeys.Contains(key))
            {
                Debug.Log($"[SettingsUI] Ignoring special key for rebind: {key.ToString()}");
                return;
            }

            ApplyUseSkillKey(key);
        }

        /// <summary>
        /// Aplica key-ul selectat cum UseSkillKey din SettingsManager si iesi din rebind.
        /// </summary>
        /// <param name="key">Nou key de skill.</param>
        private void ApplyUseSkillKey(Key key)
        {
            _isWaitingForKey = false;

            var settings = SettingsService.Instance;
            if (settings != null)
            {
                settings.SetUseSkillKey(key.ToString());
            }

            SetUseSkillKeyLabel(key.ToString());
            Debug.Log($"[SettingsUI] UseSkillKey rebound to: {key.ToString()}");
        }

        private void OnUseSkillKeyClicked()
        {
            _isWaitingForKey = true;
            SetUseSkillKeyLabel(WaitingForKeyLabel);
        }

        /// <summary>
        /// Afiseaza key-ul configurat (din settings) in textul bottonului.
        /// </summary>
        private void RefreshUseSkillKeyLabel()
        {
            var settings = SettingsService.Instance;
            string keyName = settings != null ? settings.UseSkillKey : "E";
            SetUseSkillKeyLabel(keyName);
        }

        /// <summary>
        /// Setarea textului label bottonului de rebind.
        /// </summary>
        /// <param name="text">Text de afisat.</param>
        private void SetUseSkillKeyLabel(string text)
        {
            if (_useSkillKeyLabel != null)
            {
                _useSkillKeyLabel.text = text;
            }
        }

        private void HandleMusicSliderChanged(float value)
        {
            // Sai bo qua cand seteaza din Refresh (evita aplicarea de valori intermedi cand drag animat).
            if (_isApplyingState)
            {
                return;
            }

            var settings = SettingsService.Instance;
            if (settings != null)
            {
                settings.SetMusicVolume(value);
            }
        }

        private void HandleSfxSliderChanged(float value)
        {
            if (_isApplyingState)
            {
                return;
            }

            var settings = SettingsService.Instance;
            if (settings != null)
            {
                settings.SetSfxVolume(value);
            }
        }

        private void HandleScreenShakeToggleChanged(bool isOn)
        {
            if (_isApplyingState)
            {
                return;
            }

            var settings = SettingsService.Instance;
            if (settings != null)
            {
                settings.SetScreenShakeEnabled(isOn);
            }
        }

        private void HandleScreenShakeGroupToggleChanged(bool _)
        {
            if (_isApplyingState)
            {
                return;
            }

            bool isShakeEnabled = _screenShakeOnToggle != null && _screenShakeOnToggle.isOn;
            var settings = SettingsService.Instance;
            if (settings != null)
            {
                settings.SetScreenShakeEnabled(isShakeEnabled);
            }
        }

        #endregion
    }
}