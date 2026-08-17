using UnityEngine;

namespace Bombs.Data
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
        public float fallSpeed = 10f; // Giữ lại public field cho Inspector
        public float FallSpeed => fallSpeed; // Triển khai interface (nếu có)
        
        [Header("Hiệu ứng")]
        [Tooltip("Hiệu ứng hình ảnh (VFX) tạo ra tại điểm tiếp đất.")]
        public GameObject landingLightVFX;

        [Tooltip("Màu của ánh sáng chỉ báo điểm rơi. Chỉ có tác dụng nếu VFX có component 'Light'.")]
        [ColorUsage(true, true)] // Cho phép chọn màu HDR trong Inspector
        public Color landingLightColor = Color.white;

    }
}