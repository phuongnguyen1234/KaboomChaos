using System;

namespace Core
{
    /// <summary>
    /// Định nghĩa tất cả các event toàn cục trong game.
    /// Sử dụng static events để các hệ thống khác có thể đăng ký và phản hồi.
    /// </summary>
    public static class GameEvents
    {
        #region Action declaration
        /// <summary>
        /// Được gọi khi một người chơi chết.
        /// </summary>
        public static event Action OnPlayerDied;

        #endregion

        #region Invoked methods
        public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();

        #endregion
    }
}
