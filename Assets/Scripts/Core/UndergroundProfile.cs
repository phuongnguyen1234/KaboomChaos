using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dữ liệu cho một tầng địa chất, bao gồm prefab và chiều cao của nó.
/// </summary>
[System.Serializable]
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
[CreateAssetMenu(fileName = "NewUndergroundProfile", menuName = "Kaboom Chaos/Underground Profile")]
public class UndergroundProfile : ScriptableObject
{
    [Header("Cấu hình Grid")]
    [Tooltip("Kích thước của lưới theo trục X và Z.")]
    public Vector2Int gridSize = new(10, 10);

    [Tooltip("Khoảng cách thêm vào giữa các khối.")]
    public Vector3 spacing;

    [Header("Các tầng địa chất")]
    [Tooltip("Danh sách các tầng địa chất. Tầng đầu tiên trong danh sách sẽ ở trên cùng (ngay dưới mặt đất).")]
    public List<GeologicalLayer> layers = new();

    [Header("Ngẫu nhiên")]
    [Tooltip("Hạt giống (seed) để tạo sự ngẫu nhiên. Thay đổi giá trị này để có một mô hình phân bố khác.")]
    public int randomSeed = 0;
}