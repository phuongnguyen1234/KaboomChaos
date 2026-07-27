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
        /// Animation (di chuyển, mờ dần) được xử lý nội bộ bởi controller.
        /// </summary>
        /// <param name="text">Nội dung để hiển thị.</param>
        /// <param name="color">Màu sắc của text.</param>
        void Trigger(string text, Color color);
    }
}