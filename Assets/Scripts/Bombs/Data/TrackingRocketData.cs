using UnityEngine;

namespace Bombs.Data
{
    /// <summary>
    /// Enum để xác định cách tên lửa tìm và theo dõi mục tiêu.
    /// </summary>
    public enum TrackingType
    {
        /// <summary>
        /// Tìm một người chơi ngẫu nhiên khi được tạo và khóa mục tiêu đó.
        /// Sẽ tìm mục tiêu mới nếu mục tiêu hiện tại chết.
        /// </summary>
        Homing,
        /// <summary>
        /// Luôn luôn tìm và bay về phía người chơi gần nhất.
        /// </summary>
        Chasing
    }

    /// <summary>
    /// Dữ liệu cho các loại bom tên lửa có khả năng theo dõi mục tiêu.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTrackingRocketData", menuName = "Kaboom Chaos/Bomb Types/Tracking Rocket")]
    public class TrackingRocketData : BombData
    {
        [Header("Hành vi Tên lửa Theo dõi")]
        [Tooltip("Loại hình theo dõi mục tiêu.")]
        public TrackingType trackingType = TrackingType.Homing;

        [Tooltip("Tốc độ di chuyển của tên lửa (đơn vị/giây).")]
        public float speed = 15f;

        [Header("Target Lock VFX")]
        [Tooltip("Prefab VFX hien thi hieu ung Target Lock tren world space (co component WorldTargetLockController).")]
        public GameObject targetLockVFXPrefab;
    }
}