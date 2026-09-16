using UnityEngine;

namespace UI
{
    /// <summary>
    /// Popup hien thi thong tin ve tro choi (huong dan, luat choi, credits...).
    /// Ke thua tu BasePopup de ho tro day du animation slide in/out va nut dong.
    /// </summary>
    public class InfoPopup : BasePopup
    {
        #region Unity Lifecycle
        /// <summary>
        /// Khoi tao popup va gan su kien cho nut dong.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }
        #endregion

        #region Protected Virtual Hooks
        /// <summary>
        /// Hook duoc goi ngay sau khi popup Info duoc mo.
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();
        }

        /// <summary>
        /// Hook duoc goi ngay truoc khi popup Info duoc dong.
        /// </summary>
        protected override void OnHidden()
        {
            base.OnHidden();
        }
        #endregion
    }
}

