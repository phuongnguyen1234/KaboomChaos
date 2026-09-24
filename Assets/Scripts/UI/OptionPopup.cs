using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core;

namespace UI
{
    /// <summary>
    /// Popup cai dat (Option) khong co tab.
    /// Trang: gom nu dong va khu vuc noi dung cai dat.
    /// Hien tai co: toggle bat/tat Extreme Mode (duoc gan vao mot ToggleGroup).
    /// Khi bat Extreme Mode: perk bi vô hiệu, player chi co 35 HP, score x1.25.
    /// </summary>
    public class OptionPopup : BasePopup
    {
        #region Fields

        [Header("Extreme Mode")]
        [Tooltip("Toggle bat/tat Extreme Mode tren Option Menu.")]
        [SerializeField] private Toggle _extremeModeToggle;

        [Tooltip("(Tuy chon) ToggleGroup chua toggle Extreme Mode. Neu gan, toggle se duoc gan vao group nay trong Awake.")]
        [SerializeField] private ToggleGroup _extremeModeToggleGroup;

        [Tooltip("(Tuy chon) Text mo ta trong trang thai Extreme Mode (vi du: 'Perk bi vo hieu, max HP = 35, diem x1.25').")]
        [SerializeField] private TextMeshProUGUI _extremeModeDescriptionText;

        [Header("AFK Mode")]
        [Tooltip("Toggle 'Playing' (player KHONG AFK, tham gia arena). Neu AFK TAT thi toggle nay duoc chon.")]
        [SerializeField] private Toggle _afkPlayingToggle;

        [Tooltip("Toggle 'Not Playing' (player AFK, khong dua vao arena). Neu AFK BAT thi toggle nay duoc chon.")]
        [SerializeField] private Toggle _afkNotPlayingToggle;

        [Tooltip("(Tuy chon) ToggleGroup chua cac toggle AFK. Neu gan, cac toggle se duoc gan vao group nay trong Awake.")]
        [SerializeField] private ToggleGroup _afkToggleGroup;

        // Guard tranh tinh cach nhau trong Process chua cac gia tri da luu khi hien popup.
        private bool _isApplyingSavedState;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Gan su kien cho toggle Extreme Mode. Goi base.Awake de wire nu dong popup.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (_extremeModeToggle == null)
            {
                Debug.LogWarning("[OptionPopup] _extremeModeToggle chua duoc gan tren Inspector. Khong the bat/tat Extreme Mode.", this);
                return;
            }

            // Neu co ToggleGroup, gan toggle vao group (ho tro toggle group theo yeu cau).
            if (_extremeModeToggleGroup != null)
            {
                _extremeModeToggle.group = _extremeModeToggleGroup;
            }

            _extremeModeToggle.onValueChanged.AddListener(HandleExtremeModeToggleChanged);

            // Wire cac toggle AFK (neu co). Tham chieu CAI HAI toggle (Playing / Not Playing)
            // de tranh ambiguity voi ToggleGroup mutuellay rauful.
            if (_afkPlayingToggle == null || _afkNotPlayingToggle == null)
            {
                Debug.LogWarning("[OptionPopup] _afkPlayingToggle hoac _afkNotPlayingToggle chua duoc gan tren Inspector. Khong the bat/tat AFK.", this);
            }
            else
            {
                if (_afkToggleGroup != null)
                {
                    _afkPlayingToggle.group = _afkToggleGroup;
                    _afkNotPlayingToggle.group = _afkToggleGroup;
                }

                // Neu both duoc gan, chi lang nghe cac 2 de poc bao khi click.
                _afkPlayingToggle.onValueChanged.AddListener(HandleAfkToggleChanged);
                _afkNotPlayingToggle.onValueChanged.AddListener(HandleAfkToggleChanged);
            }
        }

        #endregion

        #region Protected Virtual Hooks

        /// <summary>
        /// Khi mo popup, khoi phuc trang thai toggle tu du lieu da luu tru.
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();

            if (_extremeModeToggle == null) return;

            bool enabled = GameEvents.TriggerRequestExtremeModeEnabled();

            // Gan isOn se kich hoat onValueChanged; dung guard de tranh goi Save lan nua.
            _isApplyingSavedState = true;
            _extremeModeToggle.isOn = enabled;
            _isApplyingSavedState = false;

            UpdateDescription(enabled);

            // Khoi phuc trang thai cac toggle AFK tu trang thai da luu tru.
            // Mac dinh AFK = TAT -> "Playing" duoc chon (player khong AFK).
            if (_afkNotPlayingToggle != null && _afkPlayingToggle != null)
            {
                bool afkEnabled = GameEvents.TriggerRequestAfkEnabled();
                _isApplyingSavedState = true;
                _afkPlayingToggle.isOn = !afkEnabled;
                _afkNotPlayingToggle.isOn = afkEnabled;
                _isApplyingSavedState = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Xu ly khi user bat/tat toggle Extreme Mode:
        /// gui yeu cau thay doi (PlayerDataManager se luu va phat su kien de ap dung logic/UI).
        /// </summary>
        /// <param name="isOn">True neu user bat Extreme Mode.</param>
        private void HandleExtremeModeToggleChanged(bool isOn)
        {
            // Bo qua khi dang ap dung trang thai da luu (tranh vo tinh thay doi/save lai).
            if (_isApplyingSavedState) return;

            Debug.Log($"[OptionPopup] Extreme Mode toggle: {isOn}");
            GameEvents.TriggerExtremeModeChanged(isOn);
        }

        /// <summary>
        /// Xu ly khi nguoi choi bat/tat cac toggle AFK (Playing / Not Playing):
        /// gui yeu cau thay doi (PlayerManager se luu/ cap nhat logic va UI).
        /// Trang thai AFK = neu toggle "Not Playing" dang chon (isOn = true).
        /// </summary>
        /// <param name="_">Gia tri toggle mo click (khong dung trực tiep; citaf trang thai chinh via Not Playing).</param>
        private void HandleAfkToggleChanged(bool _)
        {
            // Bo qua khi dang ap dung trang thai da luu (tranh vo tinh thay doi/save lai).
            if (_isApplyingSavedState) return;

            bool afkEnabled = _afkNotPlayingToggle != null && _afkNotPlayingToggle.isOn;
            Debug.Log($"[OptionPopup] AFK toggle: {(afkEnabled ? "BAT (Not Playing)" : "TAT (Playing)")}");
            GameEvents.TriggerAfkEnabledChanged(afkEnabled);
        }

        /// <summary>
        /// Cap nhat text mo ta theo trang thai Extreme Mode (neu co gan).
        /// </summary>
        /// <param name="enabled">True neu Extreme Mode dang bat.</param>
        private void UpdateDescription(bool enabled)
        {
            if (_extremeModeDescriptionText == null) return;

            if (enabled)
            {
                _extremeModeDescriptionText.text = "EXTREME MODE BAT\n- Perk bi vo hieu\n- Max HP = 35\n- Diem x1.25";
            }
            else
            {
                _extremeModeDescriptionText.text = "Extreme Mode TAT\n(Che do choi binh thuong)";
            }
        }

        #endregion
    }
}