using System;
using UnityEngine;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Định nghĩa tất cả các event toàn cục trong game.
    /// Sử dụng static events để các hệ thống khác có thể đăng ký và phản hồi.
    /// </summary>
    public static class GameEvents
    {
        #region Event Declarations

        #region Player Events
        /// <summary>
        /// Được gọi khi một người chơi chết.
        /// </summary>
        public static event Action<IPlayer> OnPlayerDied;
        /// <summary>
        /// Được gọi khi máu của người chơi thay đổi.
        /// </summary>
        public static event Action<IPlayer, float, float> OnPlayerHealthChanged;
        /// <summary>
        /// Được gọi khi kết thúc một round, yêu cầu các component của người chơi tự reset lại trạng thái.
        /// Ví dụ: PlayerHealth sẽ hồi đầy máu, StatusEffectReceiver sẽ xóa hiệu ứng.
        /// </summary>
        public static event Action OnRoundEndPlayerReset;
        #endregion

        #region Bomb Events
        /// <summary>
        /// Được gọi khi một quả bom muốn được trả về pool.
        /// </summary>
        public static event Action<GameObject> OnBombDespawnRequest;
        #endregion

        #region VFX Events
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

        #region UI & Floating Text Events
        /// <summary>
        /// Yêu cầu hiển thị một text nổi.
        /// Tham số: Transform (đối tượng cha để đi theo), Vector3 (vị trí offset cục bộ), string (nội dung), Color (màu sắc).
        /// </summary>
        public static event Action<Transform, Vector3, string, Color> OnFloatingTextRequested;
        public static event Action<GameObject> OnFloatingTextDespawnRequest;
        #endregion

        #region Block & Terrain Events
        /// <summary>
        /// Được gọi khi một khối địa hình (DestructibleBlock) muốn được trả về pool.
        /// </summary>
        public static event Action<GameObject> OnBlockDespawnRequest;
        /// <summary>
        /// Yêu cầu sinh ra một khối địa hình (DestructibleBlock) từ pool.
        /// Trả về instance của khối đã được sinh ra.
        /// </summary>
        public static event Func<GameObject, Vector3, Quaternion, GameObject> OnBlockSpawnRequest;
        #endregion

        #endregion

        #region Trigger Methods

        #region Player Triggers
        public static void TriggerPlayerDied(IPlayer player) => OnPlayerDied?.Invoke(player);
        public static void TriggerPlayerHealthChanged(IPlayer player, float currentHealth, float maxHealth) => OnPlayerHealthChanged?.Invoke(player, currentHealth, maxHealth);
        public static void TriggerRoundEndPlayerReset() => OnRoundEndPlayerReset?.Invoke();
        #endregion

        #region Bomb Triggers
        public static void TriggerBombDespawnRequest(GameObject bombInstance) => OnBombDespawnRequest?.Invoke(bombInstance);
        #endregion

        #region VFX Triggers
        public static GameObject TriggerVFXSpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation) => OnVFXSpawnRequest?.Invoke(prefab, position, rotation);
        public static void TriggerVFXDespawnRequest(GameObject vfxInstance) => OnVFXDespawnRequest?.Invoke(vfxInstance);
        #endregion
        
        #region UI & Floating Text Triggers
        public static void TriggerFloatingTextRequested(Transform parent, Vector3 offset, string text, Color color) => OnFloatingTextRequested?.Invoke(parent, offset, text, color);
        public static void TriggerFloatingTextDespawnRequest(GameObject textObject) => OnFloatingTextDespawnRequest?.Invoke(textObject);
        #endregion
        
        #region Block & Terrain Triggers
        public static void TriggerBlockDespawnRequest(GameObject blockInstance) => OnBlockDespawnRequest?.Invoke(blockInstance);
        public static GameObject TriggerBlockSpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation) => OnBlockSpawnRequest?.Invoke(prefab, position, rotation);
        #endregion
        
        #region Listener Checks
        public static bool IsVFXPoolListening() => OnVFXDespawnRequest != null;
        public static bool IsBlockPoolListening() => OnBlockDespawnRequest != null;
        #endregion
        
        #endregion
    }
}
