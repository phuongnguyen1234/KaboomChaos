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
        GameObject GameObject { get; }

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

        /// <summary>
        /// Di chuyển người chơi đến một vị trí mới một cách an toàn, đồng thời reset lại các lực tác động.
        /// </summary>
        /// <param name="position">Vị trí thế giới mới.</param>
        void Teleport(Vector3 position);

        /// <summary>
        /// Bật hoặc tắt khả năng di chuyển của người chơi.
        /// </summary>
        /// <param name="enabled">True để bật di chuyển, False để tắt.</param>
        void SetMovementEnabled(bool enabled);
        // Bạn có thể thêm các phương thức hoặc thuộc tính khác vào đây

        /// <summary>
        /// Áp dụng một hệ số nhân vào tốc độ di chuyển của người chơi.
        /// </summary>
        void ApplySpeedMultiplier(float multiplier);

        /// <summary>
        /// Gỡ bỏ một hệ số nhân khỏi tốc độ di chuyển của người chơi.
        /// </summary>
        void RemoveSpeedMultiplier(float multiplier);

        /// <summary>
        /// Áp dụng một hệ số nhân vào lực nhảy của người chơi.
        /// </summary>
        void ApplyJumpMultiplier(float multiplier);

        /// <summary>
        /// Gỡ bỏ một hệ số nhân khỏi lực nhảy của người chơi.
        /// </summary>
        void RemoveJumpMultiplier(float multiplier);
        
        /// <summary>
        /// Cho biết người chơi có đang bị đóng băng hay không.
        /// </summary>
        bool IsFrozen { get; }

        /// <summary>
        /// Yêu cầu người chơi phát nhạc của sảnh chờ (lobby).
        /// </summary>
        void PlayLobbyMusic();

        /// <summary>
        /// Yêu cầu người chơi bắt đầu playlist nhạc gameplay dựa trên độ khó.
        /// </summary>
        /// <param name="intensity">Độ khó của round đấu.</param>
        void PlayGameplayMusic(float intensity);

        /// <summary>
        /// Yêu cầu người chơi phát nhạc cho 30 giây cuối của round đấu.
        /// </summary>
        /// <param name="intensity">Độ khó của round đấu.</param>
        void PlayLast30sMusic(float intensity);

        /// <summary>
        /// Yêu cầu người chơi dừng tất cả nhạc đang phát.
        /// </summary>
        void StopMusic();

        /// <summary>
        /// Tạm dừng nhạc nền hiện tại.
        /// </summary>
        void PauseMusic();

        /// <summary>
        /// Tiếp tục phát nhạc nền đã bị tạm dừng.
        /// </summary>
        void ResumeMusic();

        /// <summary>
        /// Reset toàn bộ chỉ số cộng dồn về trạng thái gốc.
        /// </summary>
        void ResetModifiers();

        /// <summary>
        /// Container Transform cho floating text liên quan đến HP.
        /// </summary>
        Transform HPTextContainer { get; }

        /// <summary>
        /// Container Transform cho floating text liên quan đến Coin.
        /// </summary>
        Transform CoinTextContainer { get; }
    }
}
