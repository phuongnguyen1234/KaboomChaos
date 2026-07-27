using UnityEngine;

namespace Core
{
    /// <summary>
    /// Dữ liệu cho loại bom dạng tên lửa/đạn pháo.
    /// Loại bom này sẽ rơi với một tốc độ không đổi và phát nổ khi va chạm.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMissileBombData", menuName = "Kaboom Chaos/Bomb Types/Missile Bomb")]
    public class MissileBombData : BaseBombData
    {
        [Header("Hành vi Missile")]
        [Tooltip("Tốc độ rơi không đổi của tên lửa (đơn vị/giây).")]
        public float fallSpeed = 10f;
    }
}