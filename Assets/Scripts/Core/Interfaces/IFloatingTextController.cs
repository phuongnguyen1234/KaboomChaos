using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for controlling a floating text object.
    /// This allows managers to interact with the text controller
    /// without a direct dependency on its implementation.
    /// </summary>
    public interface IFloatingTextController
    {
        /// <summary>
        /// The GameObject of the controller.
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// Kích hoạt animation của text nổi.
        /// Text chỉ cần biết nội dung và tự chạy animation trên UI.
        /// </summary>
        void Trigger(string text, Color color, bool showIcon);
    }
}