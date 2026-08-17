using UnityEngine;

namespace Core
{
    /// <summary>
    /// Điểm khởi đầu của game. Chịu trách nhiệm khởi tạo tất cả các Manager
    /// theo một thứ tự được kiểm soát để đảm bảo các dependency được giải quyết đúng.
    /// Chỉ nên có một đối tượng này trong scene khởi đầu của bạn.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [Header("Core Systems & Pools")]
        [Tooltip("Các manager hệ thống cốt lõi và các pool không có dependency chéo.")]
        [SerializeField] private GameObject _uiManagerPrefab;
        [SerializeField] private GameObject _spawnManagerPrefab;
        [SerializeField] private GameObject _destructionManagerPrefab;
        [SerializeField] private GameObject _mapManagerPrefab;
        [SerializeField] private GameObject _vfxPoolManagerPrefab;
        [SerializeField] private GameObject _blockPoolManagerPrefab;
        [SerializeField] private GameObject _floatingTextManagerPrefab;
        [SerializeField] private GameObject _undergroundGeneratorPrefab;

        [Header("Gameplay Managers")]
        [Tooltip("Các manager gameplay phụ thuộc vào các hệ thống cốt lõi.")]
        [SerializeField] private GameObject _playerManagerPrefab;
        [SerializeField] private GameObject _bombSpawnerManagerPrefab;

        [Header("Game Loop")]
        [Tooltip("Prefab cho GameloopManager. Khởi tạo cuối cùng vì nó phụ thuộc vào nhiều manager khác.")]
        [SerializeField] private GameObject _gameloopManagerPrefab;

        // Cờ static để đảm bảo quá trình khởi tạo chỉ chạy một lần duy nhất.
        private static bool _hasBeenInitialized = false;

        private void Start()
        {
            // Ngăn bootstrapper chạy lại nếu nó vô tình tồn tại trong một scene được tải lại.
            if (_hasBeenInitialized)
            {
                Destroy(gameObject);
                return;
            }

            // Khởi tạo các manager từ prefab theo thứ tự logic để dễ theo dõi.
            // Nhóm 1: Core Systems & Pools (ít hoặc không có dependency)
            Instantiate(_uiManagerPrefab);
            Instantiate(_spawnManagerPrefab);
            Instantiate(_destructionManagerPrefab);
            Instantiate(_mapManagerPrefab);
            Instantiate(_vfxPoolManagerPrefab);
            Instantiate(_blockPoolManagerPrefab);
            Instantiate(_floatingTextManagerPrefab);
            if (_undergroundGeneratorPrefab != null) Instantiate(_undergroundGeneratorPrefab);

            // Nhóm 2: Gameplay Managers (phụ thuộc vào nhóm 1)
            Instantiate(_playerManagerPrefab);
            Instantiate(_bombSpawnerManagerPrefab);

            // Nhóm 3: Game Loop (phụ thuộc vào các nhóm trên)
            Instantiate(_gameloopManagerPrefab);

            _hasBeenInitialized = true;
            Destroy(gameObject); // Bootstrapper đã hoàn thành nhiệm vụ.
        }
    }
}