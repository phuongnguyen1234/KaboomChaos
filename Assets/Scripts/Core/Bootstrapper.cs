using DG.Tweening;
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
        [Header("Audio & System Services")]
        [Tooltip("Cac manager am thanh va cai dat he thong.")]
        [SerializeField] private GameObject _settingsManagerPrefab;
        [SerializeField] private GameObject _sfxManagerPrefab;
        [SerializeField] private GameObject _bgmControllerPrefab;

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
        [SerializeField] private GameObject _playerDataManagerPrefab;
        [SerializeField] private GameObject _collectiblePoolManagerPrefab;

        [Header("Gameplay Managers")]
        [Tooltip("Các manager gameplay phụ thuộc vào các hệ thống cốt lõi.")]
        [SerializeField] private GameObject _playerManagerPrefab;
        [SerializeField] private GameObject _bombSpawnerManagerPrefab;

        [Header("Game Loop")]
        [Tooltip("Prefab cho GameloopManager. Khởi tạo cuối cùng vì nó phụ thuộc vào nhiều manager khác.")]
        [SerializeField] private GameObject _gameloopManagerPrefab;

        // Cờ static để đảm bảo quá trình khởi tạo chỉ chạy một lần duy nhất.
        private static bool _hasBeenInitialized = false;

        /// <summary>
        /// Tu dong nap Bootstrapper prefab tu Resources neu scene chua co Bootstrapper va chua duoc khoi tao.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoLoadBootstrapper()
        {
            if (_hasBeenInitialized) return;
            if (FindAnyObjectByType<Bootstrapper>() != null) return;

            var prefab = Resources.Load<GameObject>("Bootstrapper");
            if (prefab != null)
            {
                Instantiate(prefab);
            }
        }

        private void Awake()
        {
            // Ngăn bootstrapper chạy lại nếu nó vô tình tồn tại trong một scene được tải lại.
            if (_hasBeenInitialized)
            {
                Destroy(gameObject);
                return;
            }

            // Tăng giới hạn DOTween để tránh lỗi khi có nhiều VFX nổ cùng lúc
            DOTween.SetTweensCapacity(500, 50);

            // Khởi tạo các manager từ prefab theo thứ tự logic để dễ theo dõi.
            // Nhóm 0: Audio & System Services
            if (_settingsManagerPrefab != null) Instantiate(_settingsManagerPrefab);
            if (_sfxManagerPrefab != null) Instantiate(_sfxManagerPrefab);
            if (_bgmControllerPrefab != null) Instantiate(_bgmControllerPrefab);

            // Nhóm 1: Core Systems & Pools (ít hoặc không có dependency)
            if (_uiManagerPrefab != null) Instantiate(_uiManagerPrefab);
            if (_spawnManagerPrefab != null) Instantiate(_spawnManagerPrefab);
            if (_destructionManagerPrefab != null) Instantiate(_destructionManagerPrefab);
            if (_mapManagerPrefab != null) Instantiate(_mapManagerPrefab);
            if (_vfxPoolManagerPrefab != null) Instantiate(_vfxPoolManagerPrefab);
            if (_blockPoolManagerPrefab != null) Instantiate(_blockPoolManagerPrefab);
            if (_floatingTextManagerPrefab != null) Instantiate(_floatingTextManagerPrefab);
            if (_undergroundGeneratorPrefab != null) Instantiate(_undergroundGeneratorPrefab);
            if (_playerDataManagerPrefab != null) Instantiate(_playerDataManagerPrefab);
            if (_collectiblePoolManagerPrefab != null) Instantiate(_collectiblePoolManagerPrefab);

            // Nhóm 2: Gameplay Managers (phụ thuộc vào nhóm 1)
            if (_playerManagerPrefab != null) Instantiate(_playerManagerPrefab);
            if (_bombSpawnerManagerPrefab != null) Instantiate(_bombSpawnerManagerPrefab);

            // Nhóm 3: Game Loop (phụ thuộc vào các nhóm trên)
            if (_gameloopManagerPrefab != null) Instantiate(_gameloopManagerPrefab);

            _hasBeenInitialized = true;
            Destroy(gameObject); // Bootstrapper đã hoàn thành nhiệm vụ.
        }
    }
}