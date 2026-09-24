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
        /// Event emit khi user click nut Settings — parent (UIManager) mo popup Settings.
        /// </summary>
        event Action OnSettingsClicked;

        /// <summary>
        /// Event emit khi user click nut Info — parent (UIManager) mo popup Info.
        /// </summary>
        event Action OnInfoClicked;

        /// <summary>
        /// Hiển thị màn hình Home và bắt đầu chuỗi loading.
        /// </summary>
        void Show(bool skipLoading = false);

        /// <summary>
        /// Cho biet man hinh Home hien co dang hien thi hay khong.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Ẩn toàn bộ màn hình Home.
        /// </summary>
        void Hide();
    }
}