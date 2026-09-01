using UnityEngine;

namespace Bombs.Data
{
    /// <summary>
    /// Dữ liệu cấu hình cho Thủy lôi (Naval Mine).
    /// </summary>
    [CreateAssetMenu(fileName = "NewNavalMineData", menuName = "Kaboom Chaos/Bomb Types/Naval Mine")]
    public class NavalMineData : BombData
    {
        [Header("Naval Mine Behavior")]
        [Tooltip("Tốc độ trôi lên của mìn (đơn vị/giây).")]
        public float ascendSpeed = 2.0f;

        [Tooltip("Chiều dài tối thiểu của dây xích trước khi mìn có thể được kích hoạt.")]
        public float chainMinLength = 2.0f;

        [Tooltip("Chiều dài tối đa của dây xích. Mìn sẽ tự kích hoạt khi đạt đến độ dài này.")]
        public float chainMaxLength = 10.0f;

        [Tooltip("Bán kính an toàn xung quanh mìn cần phải trống trước khi nó có thể kích hoạt.")]
        public float safeRadius = 1.5f;

        [Tooltip("Prefab của dây xích (nên có component ChainController).")]
        public GameObject chainPrefab;

        [Tooltip("Lực nổi đẩy mìn lên khi nó đang trôi nổi.")]
        public float buoyancyForce = 10.0f;

        [Tooltip("Lực kéo mìn về phía điểm neo khi dây xích bị căng.")]
        public float chainTensionForce = 50.0f;

        [Header("Kích hoạt an toàn")]
        [Tooltip("Lớp đệm (clearance) cộng thêm vào kích thước collider thật của mìn để tính bán kính an toàn khi bật collider. Giúp mìn KHÔNG bị kẹt trong các khối lân cận khi kích hoạt. Bán kính an toàn thực tế = max(safeRadius, bán kính collider + lớp đệm này).")]
        public float armClearanceBuffer = 0.3f;

        [Min(1)]
        [Tooltip("Số frame liên tiếp được coi là 'an toàn' trước khi mìn được phép bật collider. Giúp tránh bật collider quá sớm khi có một khối nằm sát rìa vùng an toàn bị phát hiện chập chờn.")]
        public int safeClearFrames = 3;
    }
}