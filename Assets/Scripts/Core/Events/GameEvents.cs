using System;

namespace KaboomChaos
{
    /// <summary>
    /// Định nghĩa tất cả các event toàn cục trong game.
    /// Sử dụng static events để các hệ thống khác có thể đăng ký và phản hồi.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>
        /// Được gọi khi một người chơi chết.
        /// </summary>
        public static event Action OnPlayerDied;

        public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();
    }
}
