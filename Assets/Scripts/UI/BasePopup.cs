using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Lop goc cho tat ca popup.
    /// Quan ly cac hanh vi Show/Hide/Toggle va nu dong popup.
    /// Khong phu thuoc vao noi dung cu the cua tung popup.
    /// </summary>
    public abstract class BasePopup : MonoBehaviour
    {
        #region Fields
        [Tooltip("Nu dong popup. Co the de trong neu popup khong can nu dong.")]
        [SerializeField] private Button _closeButton;
        #endregion

        #region Properties
        /// <summary>
        /// Tra ve true khi popup dang hien thi.
        /// </summary>
        public virtual bool IsVisible => gameObject.activeSelf;
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Gan su kien cho nu dong. Lua y: popup nen duoc tao san o trang thai INACTIVE
        /// trong prefab/scene de khong hien len luc boot game.
        /// </summary>
        protected virtual void Awake()
        {
            WireCloseButton();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Hien thi popup.
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnShow();
        }

        /// <summary>
        /// An popup.
        /// </summary>
        public virtual void Hide()
        {
            OnHidden();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Dong/mo popup theo trang thai hien tai.
        /// </summary>
        public virtual void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }
        #endregion

        #region Protected Virtual Hooks
        /// <summary>
        /// Hook duoc goi ngay sau khi popup duoc mo. Class con override de chua logic rieng.
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// Hook duoc goi ngay truoc khi popup duoc dong. Class con override de cleanup.
        /// </summary>
        protected virtual void OnHidden()
        {
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Gan lang nghe su kien click cho nu dong (neu co).
        /// </summary>
        private void WireCloseButton()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }
        }
        #endregion
    }
}