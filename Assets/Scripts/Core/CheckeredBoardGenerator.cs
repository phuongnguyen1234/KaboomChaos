using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Tự động tạo một bề mặt dạng bàn cờ từ hai prefab được cung cấp.
/// Script này được thiết kế để hoạt động chủ yếu trong Unity Editor.
/// </summary>
[ExecuteAlways]
[SelectionBase]
public class CheckeredBoardGenerator : MonoBehaviour
{
    [Header("Cấu hình Board")]
    [Tooltip("Prefab cho ô cờ thứ nhất (ví dụ: ô trắng).")]
    [SerializeField] private GameObject _gridPrefab1;

    [Tooltip("Prefab cho ô cờ thứ hai (ví dụ: ô đen).")]
    [SerializeField] private GameObject _gridPrefab2;

    [Tooltip("Kích thước của bàn cờ theo trục X và Z.")]
    [SerializeField] private Vector2Int _boardSize = new(11, 11);

    [Tooltip("Khoảng cách thêm vào giữa các ô cờ.")]
    [SerializeField] private Vector3 _spacing = Vector3.zero;

    private Vector3 _calculatedUnitSize = Vector3.one;

    /// <summary>
    /// Tạo hoặc tái tạo lại toàn bộ bàn cờ.
    /// </summary>
    [ContextMenu("Generate Board")]
    public void GenerateBoard()
    {
        if (_gridPrefab1 == null || _gridPrefab2 == null)
        {
            Debug.LogError("Vui lòng gán cả hai prefab cho grid trước khi tạo board.", this);
            return;
        }

        ClearBoard();

        // Tính toán kích thước của một ô cờ.
        // Ưu tiên lấy từ prefab 1, nếu không được thì thử prefab 2.
        _calculatedUnitSize = GetPrefabSize(_gridPrefab1);
        if (_calculatedUnitSize == Vector3.one && _gridPrefab1.GetComponentInChildren<Renderer>(true) == null)
        {
            _calculatedUnitSize = GetPrefabSize(_gridPrefab2);
        }

        // Tính toán offset để bàn cờ được căn giữa so với đối tượng cha.
        float offsetX = (_boardSize.x - 1) / 2.0f;
        float offsetZ = (_boardSize.y - 1) / 2.0f;

        for (int x = 0; x < _boardSize.x; x++)
        {
            for (int z = 0; z < _boardSize.y; z++)
            {
                // Chọn prefab để tạo họa tiết caro.
                GameObject prefabToUse = ((x + z) % 2 == 0) ? _gridPrefab1 : _gridPrefab2;

#if UNITY_EDITOR
                var newGrid = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, transform);
#else
                var newGrid = Instantiate(prefabToUse, transform);
#endif

                Vector3 position = new(
                    (x - offsetX) * (_calculatedUnitSize.x + _spacing.x),
                    0,
                    (z - offsetZ) * (_calculatedUnitSize.z + _spacing.z)
                );

                newGrid.transform.localPosition = position;
                newGrid.name = $"Grid ({x}, {z})";
            }
        }
    }

    /// <summary>
    /// Xóa tất cả các ô cờ đã được tạo.
    /// </summary>
    [ContextMenu("Clear Board")]
    public void ClearBoard()
    {
        // Dùng vòng lặp ngược để xóa child một cách an toàn trong editor.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Tính toán kích thước thực tế của một prefab bằng cách tạo một instance tạm thời.
    /// </summary>
    private Vector3 GetPrefabSize(GameObject prefab)
    {
        if (prefab == null) return Vector3.one;

        GameObject tempInstance = null;
        try
        {
            tempInstance = Instantiate(prefab, new Vector3(10000, 10000, 10000), Quaternion.identity);
            var renderer = tempInstance.GetComponentInChildren<Renderer>(true);
            if (renderer != null)
            {
                return renderer.bounds.size;
            }
        }
        finally
        {
            if (tempInstance != null)
            {
                DestroyImmediate(tempInstance);
            }
        }
        return Vector3.one; // Fallback
    }
}