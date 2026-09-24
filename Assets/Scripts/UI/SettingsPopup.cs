using UnityEngine;

namespace UI
{
    /// <summary>
    /// Popup cai dat (Settings) chua component SettingsUI.
    /// Show hien popup va dong bo; Hide huy rebind dang thuc hien (neu nguoi dung dong popup).
    /// </summary>
    public class SettingsPopup : BasePopup
    {
        #region Fields
        [Tooltip("Component SettingsUI chua cac thiet lap. Neu khong gan se tu dong tim trong child.")]
        [SerializeField] private SettingsUI _settingsUI;
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Khoi tao popup va tu tim SettingsUI neu chua duoc gan qua Inspector.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (_settingsUI == null)
            {
                _settingsUI = GetComponentInChildren<SettingsUI>(true);
            }
        }
        #endregion

        #region Protected Virtual Hooks
        /// <summary>
        /// Khi mo popup, dong bo UI voi gia tri duoc luu trong SettingsManager.
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();

            if (_settingsUI == null)
            {
                _settingsUI = GetComponentInChildren<SettingsUI>(true);
            }

            if (_settingsUI != null)
            {
                _settingsUI.Refresh();
            }
        }

        /// <summary>
        /// Khi dong popup, huy thao tac rebind dang dien ra de tranh doi phim ngoai y muon.
        /// </summary>
        protected override void OnHidden()
        {
            base.OnHidden();

            if (_settingsUI != null)
            {
                _settingsUI.CancelRebind();
            }
        }
        #endregion
    }
}