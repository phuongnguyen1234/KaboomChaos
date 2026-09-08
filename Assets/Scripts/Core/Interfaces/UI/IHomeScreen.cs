using System;

namespace Core.Interfaces.UI
{
    /// <summary>
    /// Interface cho màn hình Home (bao gồm loading và main menu).
    /// </summary>
    public interface IHomeScreen
    {
        /// <summary>
        /// Event được emit khi người dùng click nút Play.
        /// Pattern event-based tương tự Vue: child emit event lên parent.
        /// </summary>
        event Action OnPlayClicked;

        /// <summary>
        /// Event emit cuando user click nút Settings — parent (UIManager) mo popup Settings.
        /// </summary>
        event Action OnSettingsClicked;

        /// <summary>
        /// Hiển thị màn hình Home và bắt đầu chuỗi loading.
        /// </summary>
        void Show(bool skipLoading = false);

        /// <summary>
        /// Ẩn toàn bộ màn hình Home.
        /// </summary>
        void Hide();
    }
}