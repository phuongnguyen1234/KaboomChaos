using System;
using UnityEngine;

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

        /// <summary>
        /// Được gọi khi một quả bom muốn được trả về pool.
        /// </summary>
        public static event Action<GameObject> OnBombDespawnRequest;

        /// <summary>
        /// Yêu cầu sinh ra một hiệu ứng hình ảnh (VFX).
        /// Trả về instance của VFX đã được sinh ra.
        /// </summary>
        public static event Func<GameObject, Vector3, Quaternion, GameObject> OnVFXSpawnRequest;

        /// <summary>
        /// Được gọi khi một hiệu ứng hình ảnh (VFX) muốn được trả về pool.
        /// </summary>
        public static event Action<GameObject> OnVFXDespawnRequest;

        #endregion

        #region Invoked methods
        public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();

        public static void TriggerBombDespawnRequest(GameObject bombInstance) => OnBombDespawnRequest?.Invoke(bombInstance);

        public static GameObject TriggerVFXSpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation) => OnVFXSpawnRequest?.Invoke(prefab, position, rotation);

        public static void TriggerVFXDespawnRequest(GameObject vfxInstance) => OnVFXDespawnRequest?.Invoke(vfxInstance);

        public static bool IsVFXPoolListening() => OnVFXDespawnRequest != null;
        #endregion
    }
}
