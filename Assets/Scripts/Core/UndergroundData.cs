using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Dữ liệu cho một tầng địa chất, bao gồm prefab và chiều cao của nó.
    /// </summary>
    [Serializable]
    public class GeologicalLayer
    {
        [Tooltip("Prefab cho khối của tầng địa chất này.")]
        public GameObject blockPrefab;

        [Tooltip("Chiều cao của tầng này (tính bằng số lớp khối).")]
        [Min(1)]
        public int height = 1;

        [Header("Rải rác (Scattering)")]
        [Tooltip("Bật để rải rác một loại block phụ trong tầng này.")]
        public bool enableScattering = false;

        [Tooltip("Prefab cho khối phụ được rải rác. Chỉ có tác dụng khi 'Enable Scattering' được bật.")]
        public GameObject scatterPrefab;

        [Tooltip("Tỷ lệ xuất hiện của khối phụ (0-100). Ví dụ: 35 có nghĩa là 35% số khối sẽ là khối phụ.")]
        [Range(0, 100)]
        public float scatterPercentage = 35f;
    }

    /// <summary>
    /// ScriptableObject chứa toàn bộ cấu hình để tạo ra một thế giới ngầm.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUndergroundData", menuName = "Kaboom Chaos/Underground Data")]
    public class UndergroundData : ScriptableObject
    {
        [Header("Cấu hình Grid")]
        [Tooltip("Kích thước của lưới theo trục X và Z.")]
        public Vector2Int gridSize = new(11, 11);

        [Header("Các tầng địa chất")]
        [Tooltip("Danh sách các tầng địa chất. Tầng đầu tiên trong danh sách sẽ ở trên cùng (ngay dưới mặt đất).")]
        public List<GeologicalLayer> layers = new();

        [Header("Ngẫu nhiên")]
        [Tooltip("Hạt giống (seed) để tạo sự ngẫu nhiên. Thay đổi giá trị này để có một mô hình phân bố khác.")]
        public int randomSeed = 0;

        /// <summary>
        /// Tính toán và trả về tổng chiều cao của tất cả các tầng địa chất.
        /// </summary>
        public int TotalHeight
        {
            get
            {
                int total = 0;
                foreach (var layer in layers) total += layer.height;
                return total;
            }
        }
    }
}
