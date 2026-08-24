using System;

namespace Core.Interfaces.UI
{
    /// <summary>
    /// Interface cho Pause Menu.
    /// </summary>
    public interface IPauseMenu
    {
        /// <summary>
        /// Hiển thị Pause Menu.
        /// </summary>
        void Show();

        /// <summary>
        /// Ẩn Pause Menu.
        /// </summary>
        void Hide();

        /// <summary>
        /// Bật/Tắt Pause Menu.
        /// </summary>
        void Toggle();

        /// <summary>
        /// Trạng thái hiển thị của Pause Menu.
        /// </summary>
        bool IsVisible { get; }
    }
}
