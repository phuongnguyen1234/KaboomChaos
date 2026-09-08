using UnityEngine;

namespace UI
{
    /// <summary>
    /// Popup cai dat (Settings) care hosteaza component SettingsUI.
    /// Show hien popup va sincroniza; Hide cancela orice rebind activ (dac user dong popup).
    /// </summary>
    public class SettingsPopup : BasePopup
    {
        #region Fields

        [Tooltip("Component SettingsUI care contine controlarea. Neu khong gan, se tu tim in child.")]
        [SerializeField] private SettingsUI _settingsUI;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Cheama nu dong popup (base) va tu tim SettingsUI neu khong duoc gan tren Inspector.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (_settingsUI == null)
            {
                _settingsUI = GetComponentInChildren<SettingsUI>();
            }
        }

        #endregion

        #region Protected Virtual Hooks

        /// <summary>
        /// Khi mo popup, sincroniza UI cu gia tri duoc luu in SettingsManager.
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();

            if (_settingsUI != null)
            {
                _settingsUI.Refresh();
            }
        }

        /// <summary>
        /// Khi dong popup, cancela thao taci rebind in curs de tranh key khong se schimba nevoit.
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