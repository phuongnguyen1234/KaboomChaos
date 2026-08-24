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
        /// Được gọi khi một người chơi yêu cầu tự reset (tự sát).
        /// </summary>
        public static event Action<IPlayer> OnPlayerResetRequested;
        /// <summary>
        /// Được gọi khi người chơi yêu cầu reset character (style Roblox).
        /// Không tham chiển chứa người chơi; PlayerManager sẽ quản người chơi hiện tại.
        /// </summary>
        public static event Action OnResetPlayerRequested;
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
        /// <summary>
        /// Được gọi khi một hiệu ứng trạng thái được áp dụng lên người chơi.
        /// </summary>
        public static event Action<IPlayer, StatusEffectType> OnPlayerStatusEffectApplied;
        /// <summary>
        /// Được gọi khi một hiệu ứng trạng thái trên người chơi được hoàn tác.
        /// </summary>
        public static event Action<IPlayer, StatusEffectType> OnPlayerStatusEffectReverted;
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
        public static event Action<Transform, Vector3, string, Color, Transform, bool> OnFloatingTextRequested;
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

                #region Game Loop Events
        /// <summary>
        /// Được gọi khi kết thúc một round, yêu cầu các hiệu ứng tạm thời (khí độc, điện...) tự dọn dẹp.
        /// </summary>
        public static event Action OnRoundEndCleanup;

        /// <summary>
        /// Yêu cầu bắt đầu phiên chơi game (thường từ màn hình chính).
        /// </summary>
                        public static event Action OnStartGameRequest;

        /// <summary>
        /// Yêu cầu trở về màn hình chính (từ trong game).
        /// </summary>
        public static event Action OnReturnToHomeRequest;

        /// <summary>
        /// Cờ toàn cục cho biết Pause Menu đang hiển thị hay không.
        /// Khi true, các hệ thống nhận input (camera, nhân vật) phải ngừng hoạt động.
        /// </summary>
        public static bool IsPauseMenuVisible = false;

        /// <summary>
        /// Yêu cầu tạm dừng toàn bộ nhạc nền (dùng khi khiên Magic đang phát nhạc riêng).
        /// </summary>
        public static event Action OnMusicPauseRequested;

        /// <summary>
        /// Yêu cầu tiếp tục nhạc nền đã bị tạm dừng.
        /// </summary>
        public static event Action OnMusicResumeRequested;
        #endregion

        #region Player Data Events
        /// <summary>
        /// Yêu cầu lưu tất cả dữ liệu người chơi.
        /// </summary>
        public static event Action OnSavePlayerDataRequest;

        /// <summary>
        /// Yêu cầu thêm (hoặc bớt) một lượng credits.
        /// Tham số: int (số lượng credits cần thêm).
        /// </summary>
        public static event Action<int> OnAddCreditsRequest;

        /// <summary>
        /// Được gọi khi số credits của người chơi thay đổi.
        /// Tham số: int (tổng số credits mới).
        /// </summary>
        public static event Action<int> OnCreditsChanged;

        /// <summary>
        /// Yêu cầu lấy số credits hiện tại. Trả về: int (số credits hiện tại).
        /// </summary>
        public static event Func<int> OnRequestCurrentCredits;
        #endregion

        #region Collectible Events
        /// <summary>
        /// Yêu cầu sinh ra một vật phẩm (collectible) từ pool.
        /// Trả về instance của vật phẩm đã được sinh ra.
        /// </summary>
        public static event Func<GameObject, Vector3, Quaternion, GameObject> OnCollectibleSpawnRequest;

        /// <summary>
        /// Được gọi khi một vật phẩm muốn được trả về pool.
        /// </summary>
        public static event Action<GameObject> OnCollectibleDespawnRequest;
        #endregion

        #endregion

        #region Trigger Methods

                #region Player Triggers
        public static void TriggerPlayerResetRequested(IPlayer player) => OnPlayerResetRequested?.Invoke(player);
        public static void TriggerResetPlayerRequested() => OnResetPlayerRequested?.Invoke();
        public static void TriggerPlayerDied(IPlayer player) => OnPlayerDied?.Invoke(player);

        public static void TriggerPlayerHealthChanged(IPlayer player, float currentHealth, float maxHealth) => OnPlayerHealthChanged?.Invoke(player, currentHealth, maxHealth);
        public static void TriggerRoundEndPlayerReset() => OnRoundEndPlayerReset?.Invoke();
        public static void TriggerPlayerStatusEffectApplied(IPlayer player, StatusEffectType effect) => OnPlayerStatusEffectApplied?.Invoke(player, effect);
        public static void TriggerPlayerStatusEffectReverted(IPlayer player, StatusEffectType effect) => OnPlayerStatusEffectReverted?.Invoke(player, effect);
        #endregion

        #region Bomb Triggers
        public static void TriggerBombDespawnRequest(GameObject bombInstance) => OnBombDespawnRequest?.Invoke(bombInstance);
        #endregion

        #region VFX Triggers
        public static GameObject TriggerVFXSpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation) => OnVFXSpawnRequest?.Invoke(prefab, position, rotation);
        public static void TriggerVFXDespawnRequest(GameObject vfxInstance) => OnVFXDespawnRequest?.Invoke(vfxInstance);
        #endregion
        
        #region UI & Floating Text Triggers
        public static void TriggerFloatingTextRequested(Transform parent, Vector3 offset, string text, Color color, Transform containerOverride = null, bool showIcon = false) => OnFloatingTextRequested?.Invoke(parent, offset, text, color, containerOverride, showIcon);
        public static void TriggerFloatingTextDespawnRequest(GameObject textObject) => OnFloatingTextDespawnRequest?.Invoke(textObject);
        #endregion
        
        #region Block & Terrain Triggers
        public static void TriggerBlockDespawnRequest(GameObject blockInstance) => OnBlockDespawnRequest?.Invoke(blockInstance);
        public static GameObject TriggerBlockSpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation) => OnBlockSpawnRequest?.Invoke(prefab, position, rotation);
        #endregion

        #region Game Loop Triggers
        public static void TriggerRoundEndCleanup() => OnRoundEndCleanup?.Invoke();
        public static void TriggerStartGameRequest() => OnStartGameRequest?.Invoke();
        public static void TriggerReturnToHomeRequest() => OnReturnToHomeRequest?.Invoke();

        public static void TriggerMusicPauseRequested() => OnMusicPauseRequested?.Invoke();
        public static void TriggerMusicResumeRequested() => OnMusicResumeRequested?.Invoke();
        #endregion

        #region Player Data Triggers
        public static void TriggerSavePlayerDataRequest() => OnSavePlayerDataRequest?.Invoke();
        public static void TriggerAddCreditsRequest(int amount) => OnAddCreditsRequest?.Invoke(amount);
        public static void TriggerCreditsChanged(int newTotal) => OnCreditsChanged?.Invoke(newTotal);
        public static int TriggerRequestCurrentCredits() => OnRequestCurrentCredits?.Invoke() ?? 0;
        #endregion

        #region Collectible Triggers
        public static GameObject TriggerCollectibleSpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation) => OnCollectibleSpawnRequest?.Invoke(prefab, position, rotation);
        public static void TriggerCollectibleDespawnRequest(GameObject collectibleInstance) => OnCollectibleDespawnRequest?.Invoke(collectibleInstance);
        #endregion
        
        #region Listener Checks
        public static bool IsVFXPoolListening() => OnVFXDespawnRequest != null;
        public static bool IsBlockPoolListening() => OnBlockDespawnRequest != null;
        #endregion
        
        #endregion
    }
}
