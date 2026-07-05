using UnityEngine; // Cần dùng Vector2 và các kiểu Unity khác

namespace Core.Interfaces
{
    /// <summary>
    /// Định nghĩa các thuộc tính và hành vi cơ bản của người chơi
    /// mà các hệ thống khác có thể cần tương tác.
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// Tham chiếu đến GameObject của người chơi.
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// Lấy tốc độ di chuyển hiện tại của người chơi.
        /// </summary>
        float CurrentSpeed { get; }

        /// <summary>
        /// Cho biết người chơi có đang ở trên mặt đất hay không.
        /// </summary>
        bool IsGrounded { get; }

        /// <summary>
        /// Cho biết người chơi có đang di chuyển hay không.
        /// </summary>
        bool IsMoving { get; }
        
        /// <summary>
        /// Vận tốc hiện tại theo trục Y của người chơi.
        /// </summary>
        float VerticalVelocity { get; }
        
        /// <summary>
        /// Tốc độ di chuyển ngang hiện tại của người chơi (không bao gồm vận tốc theo trục Y).
        /// </summary>
        float HorizontalSpeed { get; }

        /// <summary>
        /// Trả về 'true' trong một frame duy nhất ngay sau khi người chơi tiếp đất.
        /// Hữu ích cho các sự kiện animation hoặc âm thanh cần kích hoạt một lần.
        /// </summary>
        bool JustLanded { get; }

        /// <summary>
        /// Cho biết người chơi có đang leo trèo hay không.
        /// </summary>
        bool IsClimbing { get; }

        /// <summary>
        /// Tốc độ leo trèo hiện tại (từ -1 đến 1, dương là lên, âm là xuống).
        /// </summary>
        float ClimbingSpeed { get; }

        // Bạn có thể thêm các phương thức hoặc thuộc tính khác vào đây
        // Ví dụ: Vector3 GetPosition();
        // Ví dụ: void TakeDamage(float amount);
    }
}
