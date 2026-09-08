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
    /// Panel cai dat reusable (Settings): slider Music, slider SFX va nhu t rebind UseSkillKey.
    /// Lua gia tri cu singleton SettingsManager (Core) de restaurare khi vao game.
    /// Poate fi folosi in orice panel/popup (SettingsPopup, PauseMenu, MainUI).
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
        [Tooltip("Nut rebind key dung de kich hoat Skill. Khi an, text chuyen sang 'Press a Key...'.")]
        [SerializeField] private Button _useSkillKeyButton;
        [Tooltip("(Tuy chon) Text label de pe nut. Neu khong gan, se tu tim object con.")]
        [SerializeField] private TextMeshProUGUI _useSkillKeyLabel;

        // Trang thai cho doi nhan key.
        private bool _isWaitingForKey;

        // Guard tranh tinh cach sau khi Refresh().
        private bool _isApplyingState;

        // Set key speciale (khong valid de rebind), comum pentru tut instance.
        private static readonly HashSet<Key> _specialKeys = new HashSet<Key>();

        // Text hiển thi text khi rebind.
        private static readonly string WaitingForKeyLabel = "Press a Key...";

        #endregion

        #region Properties

        /// <summary>
        /// Tra ve true khi dang doi nhan key.
        /// </summary>
        public bool IsWaitingForKey => _isWaitingForKey;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            //SetupSpecialKeys();

            // Dupa nu tut de label gan, tu tim din child Text al button.
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
        }

        /// <summary>
        /// In Update: dang asteptam de o key, verificar care key ai fost apasat tren
        /// InputSystem Keyboard.current pentru a rebind.
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

            // Cauta key apasat pe acest frame. Key.values() contine tot Key-ur
            // (khong contine anyKey sintetic).
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (keyboard[key].wasPressedThisFrame)
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
        /// Dong bo UI voi gia tri din SettingsManager (apelat khi mo popup/panel).
        /// </summary>
        public void Refresh()
        {
            var settings = SettingsManager.Instance;

            _isApplyingState = true;
            if (_musicSlider != null && settings != null)
            {
                _musicSlider.value = settings.MusicVolume;
            }
            if (_sfxSlider != null && settings != null)
            {
                _sfxSlider.value = settings.SfxVolume;
            }
            _isApplyingState = false;

            RefreshUseSkillKeyLabel();
        }

        /// <summary>
        /// Cancela thao taci rebind dang dien ra (khi dong popup/panel) va reset text button.
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
        /// Tao danh sach key speciale/modifier/system (khong duoc dung cho rebind skill).
        /// Escape duoc xu ly rieng de cancel rebind.
        /// </summary>
        // private static void SetupSpecialKeys()
        // {
        //     if (!_specialKeys.isEmpty())
        //     {
        //         return;
        //     }

        //     // Key speciale chung de bo qua khi rebind.
        //     _specialKeys.add(Key.LeftShift);
        //     _specialKeys.add(Key.RightShift);
        //     _specialKeys.add(Key.LeftCtrl);
        //     _specialKeys.add(Key.RightCtrl);
        //     _specialKeys.add(Key.LeftAlt);
        //     _specialKeys.add(Key.RightAlt);
        //     _specialKeys.add(Key.LeftMeta);
        //     _specialKeys.add(Key.RightMeta);
        //     _specialKeys.add(Key.LeftWindows);
        //     _specialKeys.add(Key.RightWindows);
        //     _specialKeys.add(Key.LeftCommand);
        //     _specialKeys.add(Key.RightCommand);
        //     _specialKeys.add(Key.LeftApple);
        //     _specialKeys.add(Key.RightApple);
        //     _specialKeys.add(Key.ContextMenu);

        //     // Enter/Tab/Backspace.
        //     _specialKeys.add(Key.Enter);
        //     _specialKeys.add(Key.Tab);
        //     _specialKeys.add(Key.Backspace);

        //     // Navigation/lock/system.
        //     _specialKeys.add(Key.ArrowUp);
        //     _specialKeys.add(Key.ArrowDown);
        //     _specialKeys.add(Key.ArrowLeft);
        //     _specialKeys.add(Key.ArrowRight);
        //     _specialKeys.add(Key.Home);
        //     _specialKeys.add(Key.End);
        //     _specialKeys.add(Key.PageUp);
        //     _specialKeys.add(Key.PageDown);
        //     _specialKeys.add(Key.Insert);
        //     _specialKeys.add(Key.Delete);
        //     _specialKeys.add(Key.CapsLock);
        //     _specialKeys.add(Key.NumLock);
        //     _specialKeys.add(Key.ScrollLock);
        //     _specialKeys.add(Key.PrintScreen);
        //     _specialKeys.add(Key.Pause);

        //     // Function keys F1-F24.
        //     for (int i = 1; i <= 24; ++i)
        //     {
        //         _specialKeys.add(Key.valueOf("F" + i));
        //     }
        // }

        /// <summary>
        /// Xu ly key apasat khi dang cho rebind: Escape cancel, key special bi bo qua,
        /// cac key khac se duoc gan cho skill.
        /// </summary>
        /// <param name="key">Key duoc apasat.</param>
        private void HandlePressedKey(Key key)
        {
            if (key == Key.Escape)
            {
                // Escape cancel thao taci rebind.
                CancelRebind();
                return;
            }

            if (_specialKeys.Contains(key))
            {
                //Debug.Log($"[SettingsUI] Ignoring special key for rebind: {key.name()}");
                return;
            }

            ApplyUseSkillKey(key);
        }

        /// <summary>
        /// Aplica key selectat como UseSkillKey din SettingsManager si iesi din rebind.
        /// </summary>
        /// <param name="key">Nou key al skill.</param>
        private void ApplyUseSkillKey(Key key)
        {
            _isWaitingForKey = false;

            var settings = SettingsManager.Instance;
            if (settings != null)
            {
                //settings.SetUseSkillKey(key.name());
            }

            //SetUseSkillKeyLabel(key.name());
            //Debug.Log($"[SettingsUI] UseSkillKey rebound to: {key.name()}");
        }

        private void OnUseSkillKeyClicked()
        {
            _isWaitingForKey = true;
            SetUseSkillKeyLabel(WaitingForKeyLabel);
        }

        /// <summary>
        /// Hien thi key configurat (din settings) strat text button.
        /// </summary>
        private void RefreshUseSkillKeyLabel()
        {
            var settings = SettingsManager.Instance;
            string keyName = settings != null ? settings.UseSkillKey : "E";
            SetUseSkillKeyLabel(keyName);
        }

        /// <summary>
        /// Set text label button rebind.
        /// </summary>
        /// <param name="text">Text de hien thi.</param>
        private void SetUseSkillKeyLabel(string text)
        {
            if (_useSkillKeyLabel != null)
            {
                _useSkillKeyLabel.text = text;
            }
        }

        private void HandleMusicSliderChanged(float value)
        {
            // Bo qua cand sincronizare din Refresh (tranh tinh cach sau nu se rapid schimba cu Save lap tuc).
            if (_isApplyingState)
            {
                return;
            }

            var settings = SettingsManager.Instance;
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

            var settings = SettingsManager.Instance;
            if (settings != null)
            {
                settings.SetSfxVolume(value);
            }
        }

        #endregion
    }
}