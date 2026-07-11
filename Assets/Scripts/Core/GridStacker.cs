using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Tự động xếp chồng một prefab theo một lưới 3D.
/// Hoạt động trong cả Editor và Runtime.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
[SelectionBase] // Giúp chọn đối tượng gốc này dễ dàng hơn trong Scene
public class GridStacker : MonoBehaviour
{
    [Header("Cấu hình Grid")]
    [Tooltip("Prefab đơn vị sẽ được xếp chồng. Prefab này nên có MeshFilter.")]
    [SerializeField] private GameObject _unitPrefab;

    [Tooltip("Số lượng đơn vị xếp chồng theo mỗi trục (X, Y, Z).")]
    [SerializeField] private Vector3Int _gridSize = Vector3Int.one;

    [Tooltip("Khoảng cách thêm vào giữa các đơn vị. Dùng để tạo khoảng hở nếu cần.")]
    [SerializeField] private Vector3 _spacing;

    [Header("Rendering")]
    [Tooltip("Ghi đè vật liệu của tất cả các đơn vị trong grid.")]
    [SerializeField] private bool _overrideMaterial = false;

    [Tooltip("Vật liệu sẽ được áp dụng cho tất cả các đơn vị nếu 'Override Material' được bật.")]
    [SerializeField] private Material _sharedMaterial;

    [Header("Collider")]
    [Tooltip("Tự động điều chỉnh BoxCollider của đối tượng này để bao trọn toàn bộ grid.")]
    [SerializeField] private bool _fitColliderToBounds = true;

    // --- Private Fields ---
    private BoxCollider _mainCollider;
    private GameObject _previousUnitPrefab;

    private Vector3 _calculatedUnitSize = Vector3.one;
    
    // List để theo dõi các object đã tạo. Không được serialize để tránh lưu trữ rác vào scene.
    // Nó sẽ được điền lại khi script được kích hoạt (OnEnable).
    [System.NonSerialized] 
    private List<GameObject> _spawnedUnits = new();
    
    // Cờ để chỉ cập nhật khi cần thiết, tránh chạy logic nặng trong mỗi lần OnValidate.
    private bool _isDirty = false;

    private void OnEnable()
    {
        // Khi script được bật (hoặc scene được load), tìm lại các child đã được tạo trước đó.
        _mainCollider = GetComponent<BoxCollider>();
        _previousUnitPrefab = _unitPrefab;

        PopulateSpawnedUnitsList();
        
#if UNITY_EDITOR
        // Đăng ký sự kiện EditorUpdate để xử lý việc cập nhật một cách an toàn.
        EditorApplication.update += EditorUpdate;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        // Hủy đăng ký sự kiện khi script bị tắt hoặc component bị xóa.
        EditorApplication.update -= EditorUpdate;
#endif
    }

    private void OnValidate()
    {
        // Mỗi khi một giá trị trong Inspector thay đổi, OnValidate được gọi.
        // Chúng ta chỉ đánh dấu là "cần cập nhật" thay vì chạy logic ngay lập tức.
        _isDirty = true;
    }

    /// <summary>
    /// Phương thức này được gọi liên tục trong Editor.
    /// Nó sẽ kiểm tra cờ _isDirty và chỉ chạy UpdateGrid khi cần.
    /// </summary>
    private void EditorUpdate()
    {
        if (_isDirty)
        {
            UpdateGrid();
            _isDirty = false;
        }
    }

    /// <summary>
    /// Điền lại danh sách _spawnedUnits bằng cách quét các child của đối tượng này.
    /// Điều này giúp phục hồi trạng thái sau khi tải lại scene hoặc recompile code.
    /// </summary>
    private void PopulateSpawnedUnitsList()
    {
        _spawnedUnits.Clear();
        if (_unitPrefab == null) return;

        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            
            #if UNITY_EDITOR
            // Trong Editor, chỉ thêm vào nếu nó là một instance của prefab mục tiêu.
            // Điều này cho phép có các child object khác không bị ảnh hưởng.
            if (PrefabUtility.GetCorrespondingObjectFromSource(child) == _unitPrefab)
            {
                _spawnedUnits.Add(child);
            }
            #else
            // Ở runtime, chúng ta có thể giả định tất cả children là unit.
            _spawnedUnits.Add(child);
            #endif
        }
    }

    /// <summary>
    /// Cập nhật lại toàn bộ grid: xóa/thêm và định vị lại các đơn vị.
    /// </summary>
    [ContextMenu("Force Update Grid")]
    public void UpdateGrid()
    {
        if (this == null) return; // Tránh lỗi khi object bị xóa.

        // Yêu cầu 1: Nếu prefab bị thay đổi, dọn dẹp các object cũ.
        if (_unitPrefab != _previousUnitPrefab)
        {
            ClearAllUnits();
            _previousUnitPrefab = _unitPrefab;
        }

        // --- Bước 1: Kiểm tra và chuẩn bị ---
        if (_unitPrefab == null)
        {
            // Nếu không có prefab, xóa hết các unit cũ và dừng lại.
            ClearAllUnits();
            return;
        }

        // Đảm bảo grid size luôn có giá trị tối thiểu là 1.
        _gridSize.x = Mathf.Max(1, _gridSize.x);
        _gridSize.y = Mathf.Max(1, _gridSize.y);
        _gridSize.z = Mathf.Max(1, _gridSize.z);

        // --- Bước 2: Tính toán kích thước đơn vị ---
        // Lấy kích thước từ Mesh của prefab để xếp cho chính xác.
        var meshFilter = _unitPrefab.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            _calculatedUnitSize = meshFilter.sharedMesh.bounds.size;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy MeshFilter trên Unit Prefab. Sử dụng kích thước mặc định (1,1,1).", this);
            _calculatedUnitSize = Vector3.one;
        }

        // --- Bước 3: Điều chỉnh số lượng đơn vị ---
        // Dọn dẹp các unit bị null (do người dùng xóa thủ công trong Hierarchy).
        _spawnedUnits.RemoveAll(item => item == null);

        int targetCount = _gridSize.x * _gridSize.y * _gridSize.z;

        // Xóa các unit thừa.
        while (_spawnedUnits.Count > targetCount)
        {
            int lastIndex = _spawnedUnits.Count - 1;
            GameObject unitToRemove = _spawnedUnits[lastIndex];
            _spawnedUnits.RemoveAt(lastIndex);
            if (unitToRemove != null)
            {
                // Phải dùng DestroyImmediate trong Editor.
                DestroyImmediate(unitToRemove);
            }
        }

        // Thêm các unit còn thiếu.
        while (_spawnedUnits.Count < targetCount)
        {
#if UNITY_EDITOR
            // Dùng PrefabUtility để giữ liên kết với prefab gốc.
            var newUnit = (GameObject)PrefabUtility.InstantiatePrefab(_unitPrefab, transform);
            _spawnedUnits.Add(newUnit);
#else
            // Dùng Instantiate bình thường khi chạy game.
            var newUnit = Instantiate(_unitPrefab, transform);
            _spawnedUnits.Add(newUnit);
#endif
        }

        // --- Bước 4: Định vị lại tất cả các đơn vị (đã được căn giữa) ---
        // Yêu cầu 2: Tính toán offset để grid luôn được căn giữa theo trục X và Z của object cha.
        float offsetX = (_gridSize.x - 1) / 2.0f;
        float offsetZ = (_gridSize.z - 1) / 2.0f;

        int index = 0;
        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                for (int z = 0; z < _gridSize.z; z++)
                {
                    if (index < _spawnedUnits.Count)
                    {
                        GameObject unit = _spawnedUnits[index];
                        if (unit != null)
                        {
                            // Vị trí được tính toán để object cha luôn là tâm điểm ở dưới cùng của grid.
                            Vector3 position = new(
                                (x - offsetX) * (_calculatedUnitSize.x + _spacing.x),
                                y * (_calculatedUnitSize.y + _spacing.y), // Trục Y bắt đầu từ 0 (dưới đất) đi lên.
                                (z - offsetZ) * (_calculatedUnitSize.z + _spacing.z)
                            );
                            unit.transform.localPosition = position;
                            unit.name = $"{_unitPrefab.name} ({x},{y},{z})";

                            // Áp dụng material nếu được cấu hình
                            ApplyMaterialOverride(unit);
                        }
                        index++;
                    }
                }
            }
        }

        if (_fitColliderToBounds)
        {
            UpdateMainCollider();
        }
    }

    /// <summary>
    /// Xóa tất cả các unit đã được tạo bởi script này.
    /// </summary>
    [ContextMenu("Clear All Units")]
    private void ClearAllUnits()
    {
        // Dùng vòng lặp ngược để xóa child một cách an toàn.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        _spawnedUnits.Clear();

        if (_fitColliderToBounds && _mainCollider != null)
        {
            _mainCollider.size = Vector3.zero;
            _mainCollider.center = Vector3.zero;
        }
    }

    /// <summary>
    /// Áp dụng hoặc hoàn tác việc ghi đè vật liệu trên một đơn vị.
    /// </summary>
    /// <param name="unit">Đối tượng đơn vị để áp dụng vật liệu.</param>
    private void ApplyMaterialOverride(GameObject unit)
    {
        if (_unitPrefab == null) return;

        // Lấy TẤT CẢ renderer trong prefab instance, kể cả những object con đang bị tắt (inactive).
        var unitRenderers = unit.GetComponentsInChildren<Renderer>(true);
        if (unitRenderers.Length == 0) return;

        if (_overrideMaterial && _sharedMaterial != null)
        {
            // Áp dụng vật liệu ghi đè cho TẤT CẢ các slot material trên mỗi renderer.
            foreach (var renderer in unitRenderers)
            {
                // Tạo một mảng mới có cùng kích thước với mảng material hiện tại.
                var newMaterials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < newMaterials.Length; i++)
                {
                    // Điền vào mọi slot bằng material được override.
                    newMaterials[i] = _sharedMaterial;
                }
                // Gán lại mảng material đã được cập nhật.
                renderer.sharedMaterials = newMaterials;
            }
        }
        else
        {
            // Hoàn tác về vật liệu gốc từ prefab.
#if UNITY_EDITOR
            // Trong Editor, cách an toàn nhất để hoàn tác là dùng PrefabUtility.
            // Nó xử lý chính xác các trường hợp phức tạp như nested prefabs hoặc prefab variants,
            // vốn là nguyên nhân có thể gây ra lỗi "fallback về material gốc của model".
            foreach (var renderer in unitRenderers)
            {
                var serializedObject = new SerializedObject(renderer);
                var property = serializedObject.FindProperty("m_Materials");
                if (property != null)
                {
                    PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction);
                }
            }
#else
            // Ở runtime, không có PrefabUtility. Logic cũ có thể được giữ lại ở đây nếu cần,
            // nhưng việc revert material ở runtime ít phổ biến hơn và logic cũ không đáng tin cậy.
            // Vì vậy, để trống ở đây là an toàn nhất.
#endif
        }
    }

    /// <summary>
    /// Cập nhật BoxCollider chính để bao bọc toàn bộ các đơn vị đã tạo.
    /// </summary>
    private void UpdateMainCollider()
    {
        if (_mainCollider == null) return;
        if (_gridSize.x <= 0 || _gridSize.y <= 0 || _gridSize.z <= 0 || _unitPrefab == null)
        {
            _mainCollider.size = Vector3.zero;
            _mainCollider.center = Vector3.zero;
            return;
        }

        Vector3 totalSize = new(
            _gridSize.x * _calculatedUnitSize.x + Mathf.Max(0, _gridSize.x - 1) * _spacing.x,
            _gridSize.y * _calculatedUnitSize.y + Mathf.Max(0, _gridSize.y - 1) * _spacing.y,
            _gridSize.z * _calculatedUnitSize.z + Mathf.Max(0, _gridSize.z - 1) * _spacing.z
        );

        _mainCollider.size = totalSize;

        // Yêu cầu 2: Cập nhật tâm của collider.
        // Vì các object con đã được căn giữa theo trục XZ,
        // tâm của collider giờ chỉ cần dịch chuyển lên trên theo trục Y.
        _mainCollider.center = new Vector3(0, totalSize.y / 2f, 0);
    }
}
