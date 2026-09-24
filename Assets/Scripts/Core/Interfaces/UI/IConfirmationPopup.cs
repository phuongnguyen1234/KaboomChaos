using System;

namespace Core.Interfaces.UI
{
    /// <summary>
    /// Interface cho Popup xác nhận.
    /// </summary>
    public interface IConfirmationPopup
    {
        /// <summary>
        /// Thể hiện Singleton toàn cục của IConfirmationPopup.
        /// </summary>
        static IConfirmationPopup Instance { get; set; }

        /// <summary>
        /// Hiển thị popup với nội dung và callback.
        /// </summary>
        /// <param name="message">Nội dung câu hỏi xác nhận.</param>
        /// <param name="onConfirm">Hành động thực hiện khi người dùng đồng ý.</param>
        /// <param name="onCancel">Hành động thực hiện khi người dùng hủy bỏ (tùy chọn).</param>
        void Show(string message, Action onConfirm, Action onCancel = null);
        
        /// <summary>
        /// Ẩn popup.
        /// </summary>
        void Hide();
    }
}
