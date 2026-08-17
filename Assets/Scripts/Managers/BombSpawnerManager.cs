using UnityEngine;
using Core.Interfaces;
using Core;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Managers
{
    /// <summary>
    /// Quản lý việc sinh các loại bom ngẫu nhiên trong một khu vực được chỉ định.
    /// </summary>
    public class BombSpawnerManager : BaseGameObjectPoolManager, IBombSpawnerManager
    {
        public static IBombSpawnerManager Instance { get; private set; }

        [Header("Dependencies")]
        [Tooltip("Database chứa tất cả các loại bom có trong game.")]
        [SerializeField] private ScriptableObject _bombDatabaseAsset; // Sử dụng ScriptableObject để gán trong Inspector

        [Header("Spawning Settings")]
        [Tooltip("Đường cong xác định khoảng thời gian TỐI THIỂU (giây) giữa mỗi lần sinh bom, dựa trên độ khó. Trục X là độ khó, trục Y là thời gian.")]
        [SerializeField] private AnimationCurve _minSpawnIntervalByDifficulty = AnimationCurve.Linear(1, 2, 5, 0.5f);
        [Tooltip("Đường cong xác định khoảng thời gian TỐI ĐA (giây) giữa mỗi lần sinh bom, dựa trên độ khó. Trục X là độ khó, trục Y là thời gian.")]
        [SerializeField] private AnimationCurve _maxSpawnIntervalByDifficulty = AnimationCurve.Linear(1, 4, 5, 1.5f);
        [Tooltip("Đường cong xác định số lượng bom TỐI THIỂU thả mỗi lần, dựa trên độ khó. Trục X là độ khó, trục Y là số lượng.")]
        [SerializeField] private AnimationCurve _minBombsPerSpawnByDifficulty = AnimationCurve.Linear(1, 1, 5, 2);
        [Tooltip("Đường cong xác định số lượng bom TỐI ĐA thả mỗi lần, dựa trên độ khó. Trục X là độ khó, trục Y là số lượng.")]
        [SerializeField] private AnimationCurve _maxBombsPerSpawnByDifficulty = AnimationCurve.Linear(1, 1, 5, 5);

        // Scene References (obtained from SceneObjectRegistry)
        private BoxCollider _spawnArea;

        private Coroutine _spawnCoroutine; // Coroutine cho việc sinh bom
        private IPlayerManager _playerManager; // Tham chiếu đến PlayerManager
        private IGameloopManager _gameloopManager; // Tham chiếu đến GameloopManager
        
        private IBombDatabase _bombDatabase; // Sử dụng interface cho BombDatabase
        // Set để lưu trữ các bomb là biến thể, giúp tối ưu việc loại bỏ chúng khỏi danh sách spawn gốc.
        private readonly HashSet<IBaseBombData> _variantBombs = new(); // Sử dụng interface

        // Dictionary để theo dõi số lượng bom đang hoạt động của mỗi loại prefab, giúp tối ưu hóa việc đếm.
        private readonly Dictionary<GameObject, int> _activeBombCounts = new();

        protected override void Awake()
        {
            base.Awake(); // Gọi Awake của lớp cơ sở
            if (Instance != null && Instance as MonoBehaviour != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            // Lấy tham chiếu đến các manager khác trong Start() để đảm bảo các Singleton của chúng đã được khởi tạo trong Awake().
            // Điều này tránh được các lỗi NullReferenceException do thứ tự thực thi script không xác định.
            _playerManager = PlayerManager.Instance;
            _gameloopManager = GameloopManager.Instance;

            if (_bombDatabaseAsset is IBombDatabase bombDatabase)
            {
                _bombDatabase = bombDatabase;
            }
            else Debug.LogError("[BombSpawnerManager] Bomb Database Asset không phải là IBombDatabase hoặc chưa được gán.", this);

            PrewarmPools();
            PreprocessVariants();

            // Lấy tham chiếu đến các đối tượng trong scene từ Registry
            var registry = SceneObjectRegistry.Instance;
            if (registry != null)
            {
                _spawnArea = registry.BombSpawnArea;
            }
            else Debug.LogError("[BombSpawnerManager] SceneObjectRegistry.Instance is null!", this);
        }

        private void OnEnable()
        {
            GameEvents.OnBombDespawnRequest += HandleBombDespawn;
        }

        private void OnDisable()
        {
            GameEvents.OnBombDespawnRequest -= HandleBombDespawn;
        }

        public void StartSpawning()
        {
            if (_spawnCoroutine != null) StopSpawning();
            _spawnCoroutine = StartCoroutine(SpawnBombRoutine());
            Debug.Log("[BombSpawnerManager] Started spawning bombs.");
        }

        public void StopSpawning()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
                Debug.Log("[BombSpawnerManager] Stopped spawning bombs.");
            }
        }

        private IEnumerator SpawnBombRoutine()
        {
            if (_bombDatabase == null || _bombDatabase.Bombs.Count == 0)
            {
                Debug.LogError("[BombSpawnerManager] BombDatabase is not assigned or is empty!", this);
                yield break;
            }
            if (_spawnArea == null)
            {
                Debug.LogError("[BombSpawnerManager] Spawn Area (BoxCollider) is not assigned!", this);
                yield break;
            }

            while (true)
            {
                float currentDifficulty = _gameloopManager.CurrentIntensity;
                // 1. Chờ một khoảng thời gian ngẫu nhiên cho đợt spawn tiếp theo
                float minInterval = _minSpawnIntervalByDifficulty.Evaluate(currentDifficulty);
                float maxInterval = _maxSpawnIntervalByDifficulty.Evaluate(currentDifficulty);
                float randomInterval = Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(randomInterval);

                // 2. Xác định số lượng bom sẽ thả trong đợt này
                int minBombs = Mathf.RoundToInt(_minBombsPerSpawnByDifficulty.Evaluate(currentDifficulty));
                int maxBombs = Mathf.RoundToInt(_maxBombsPerSpawnByDifficulty.Evaluate(currentDifficulty));
                // Đảm bảo min không lớn hơn max nếu cấu hình curve bị lỗi
                if (minBombs > maxBombs) minBombs = maxBombs;
                int bombsToSpawn = Random.Range(minBombs, maxBombs + 1); // max của Range(int, int) là exclusive

                // Dictionary để theo dõi số lượng bom mỗi loại đã được sinh ra trong đợt này.
                var waveSpawnCount = new Dictionary<IBaseBombData, int>();

                // 3. Thả số lượng bom đã xác định, mỗi quả có thể là một loại khác nhau
                for (int i = 0; i < bombsToSpawn; i++)
                {
                    IBaseBombData selectedBombData = GetRandomBombData(waveSpawnCount);
                    if (selectedBombData == null)
                    {
                        Debug.LogWarning("[BombSpawnerManager] Could not select a valid bomb to spawn (check weights and limits for current difficulty). Skipping this bomb.");
                        continue; // Bỏ qua quả bom này, nhưng vẫn tiếp tục vòng lặp của đợt thả
                    }

                    GameObject bombInstance = GetFromPool(selectedBombData.BombPrefab, GetRandomSpawnPosition(selectedBombData), Quaternion.identity);
                    if (bombInstance == null) continue;

                    // Cập nhật số lượng đã spawn trong đợt này
                    waveSpawnCount[selectedBombData] = waveSpawnCount.GetValueOrDefault(selectedBombData) + 1;

                    if (bombInstance.TryGetComponent<IBombController>(out var bombController)) bombController.Activate();
                }
            }
        }

        /// <summary>
        /// Khởi tạo và làm đầy sẵn các pool bom dựa trên BombDatabase.
        /// </summary>
        private void PrewarmPools()
        {
            if (_bombDatabase == null) return;

            foreach (var bombData in _bombDatabase.Bombs.Where(b => b.BombPrefab != null))
            {
                // Lấy ra và trả lại ngay lập tức để khởi tạo pool với số lượng ban đầu.
                var instances = new List<GameObject>();
                for (int i = 0; i < _initialPoolSize; i++)
                {
                    instances.Add(GetFromPool(bombData.BombPrefab, Vector3.zero, Quaternion.identity));
                }
                foreach (var instance in instances)
                {
                    HandleBombDespawn(instance); // SỬA LỖI: Gọi HandleBombDespawn để giảm bộ đếm active.
                }
            }
        }

        private IBaseBombData GetRandomBombData(Dictionary<IBaseBombData, int> waveSpawnCount)
        {
            float currentDifficulty = _gameloopManager.CurrentIntensity;
            // --- Bước 1: Chọn một loại bom cơ bản dựa trên trọng số ---
            var weightedList = new List<(IBaseBombData data, float weight)>();
            float totalWeight = 0f;

            foreach (var bombData in _bombDatabase.Bombs)
            {
                // Bỏ qua các prefab null và các bomb được định nghĩa là biến thể của một bomb khác.
                // Điều này đảm bảo chúng không được chọn một cách độc lập.
                if (bombData.BombPrefab == null) continue;
                if (_variantBombs.Contains(bombData)) continue;

                // --- KIỂM TRA GIỚI HẠN MỚI ---
                // 1. Kiểm tra giới hạn số lượng active toàn cục
                if (bombData.MaxActiveInstances > 0 && GetActiveCount(bombData) >= bombData.MaxActiveInstances)
                {
                    continue;
                }
                // 2. Kiểm tra giới hạn số lượng trong một đợt spawn
                if (bombData.MaxPerSpawnWave > 0 && waveSpawnCount.GetValueOrDefault(bombData) >= bombData.MaxPerSpawnWave)
                {
                    continue;
                }
                // --- KẾT THÚC KIỂM TRA ---

                float weight = bombData.SpawnWeightByDifficulty.Evaluate(currentDifficulty);
                if (weight > 0)
                {
                    weightedList.Add((bombData, weight));
                    totalWeight += weight;
                }
            }

            if (totalWeight == 0) return null;

            // Chọn một quả bom gốc
            float randomValue = Random.Range(0, totalWeight);
            IBaseBombData selectedBombData = null;
            foreach (var (data, weight) in weightedList)
            {
                if (randomValue < weight)
                {
                    selectedBombData = data;
                    break;
                }
                randomValue -= weight;
            }

            if (selectedBombData == null) return null; // Fallback

            // --- Bước 2: Kiểm tra xem có nên thay thế bằng một trong các biến thể (variant) không ---
            // Duyệt qua danh sách các biến thể có thể có.
            // Thứ tự trong danh sách có ý nghĩa, biến thể ở trên sẽ được kiểm tra trước.
            if (selectedBombData.PossibleVariants != null && selectedBombData.PossibleVariants.Count > 0)
            { // Sử dụng PossibleVariants từ interface
                foreach (var variantInfo in selectedBombData.PossibleVariants)
                {
                    if (variantInfo == null || variantInfo.VariantBombData == null || variantInfo.VariantBombData.BombPrefab == null)
                    {
                        continue; // Bỏ qua biến thể không hợp lệ
                    }

                    var variantData = variantInfo.VariantBombData;

                    // --- KIỂM TRA GIỚI HẠN MỚI CHO BIẾN THỂ ---
                    // 1. Kiểm tra giới hạn số lượng active toàn cục
                    if (variantData.MaxActiveInstances > 0 && GetActiveCount(variantData) >= variantData.MaxActiveInstances)
                    {
                        continue;
                    }
                    // 2. Kiểm tra giới hạn số lượng trong một đợt spawn
                    if (variantData.MaxPerSpawnWave > 0 && waveSpawnCount.GetValueOrDefault(variantData) >= variantData.MaxPerSpawnWave)
                    {
                        continue;
                    }
                    // --- KẾT THÚC KIỂM TRA ---

                    // Lấy xác suất xuất hiện của biến thể này dựa trên độ khó hiện tại.
                    float variantChance = variantInfo.SpawnChanceByDifficulty.Evaluate(currentDifficulty);
                    
                    // Tung xúc xắc (0-100)
                    if (Random.Range(0f, 100f) < variantChance)
                    {
                        // Nếu may mắn, trả về biến thể này và dừng việc kiểm tra các biến thể khác.
                        return variantInfo.VariantBombData;
                    }
                }
            }

            // Nếu không có biến thể nào được chọn, trả về bom gốc.
            return selectedBombData;
        }

        /// <summary>
        /// Đếm số lượng instance đang hoạt động của một loại bom cụ thể.
        /// </summary>
        /// <param name="bombData">Dữ liệu của loại bom cần đếm.</param>
        /// <returns>Số lượng instance đang hoạt động.</returns>
        private int GetActiveCount(IBaseBombData bombData)
        {
            if (bombData?.BombPrefab == null) return 0;
            return _activeBombCounts.GetValueOrDefault(bombData.BombPrefab, 0);
        }

        private Vector3 GetRandomSpawnPosition(IBaseBombData bombData)
        {
            // Kiểm tra xác suất spawn gần người chơi
            if (_playerManager != null && _playerManager.GetAllPlayers().Count > 0)
            {
                // Sử dụng khoảng xác suất min/max thay vì AnimationCurve
                float randomPlayerSpawnChance = Random.Range(bombData.MinPlayerSpawnChance, bombData.MaxPlayerSpawnChance);
                if (Random.Range(0f, 100f) < randomPlayerSpawnChance)
                {
                    Vector3 playerPos = _playerManager.GetRandomPlayerPosition(); // Giả định IPlayerManager có GetRandomPlayerPosition()
                    // Thêm offset ngẫu nhiên trong bán kính playerSpawnRadius
                    Vector2 randomOffset = Random.insideUnitCircle * bombData.PlayerSpawnRadius;
                    // Spawn ở độ cao của khu vực spawn chính, nhưng vị trí XZ gần người chơi
                    float spawnX = Mathf.Clamp(playerPos.x + randomOffset.x, _spawnArea.bounds.min.x, _spawnArea.bounds.max.x);
                    float spawnZ = Mathf.Clamp(playerPos.z + randomOffset.y, _spawnArea.bounds.min.z, _spawnArea.bounds.max.z);
                    return new Vector3(spawnX, _spawnArea.bounds.max.y, spawnZ);
                }
            }

            // Fallback: Spawn trong khu vực spawn mặc định
            Bounds bounds = _spawnArea.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            return new Vector3(randomX, bounds.max.y, randomZ);
        }
        /// <summary>
        /// Xây dựng một danh sách các bomb là biến thể để loại bỏ chúng khỏi vòng quay spawn gốc.
        /// </summary>
        private void PreprocessVariants()
        {
            _variantBombs.Clear();
            if (_bombDatabase == null || _bombDatabase.Bombs == null) return;

            foreach (var bombData in _bombDatabase.Bombs)
            {
                if (bombData != null && bombData.PossibleVariants != null)
                {
                    foreach (var variant in bombData.PossibleVariants)
                    {
                        if (variant != null && variant.VariantBombData != null)
                            _variantBombs.Add(variant.VariantBombData);
                    }
                }
            }
        }

        /// <summary>
        /// Trả tất cả các quả bom đang hoạt động về lại pool.
        /// </summary>
        public void ClearAllBombs()
        {
            // Lớp cơ sở theo dõi các instance đang hoạt động thông qua _instanceToPrefabMap.
            // Chúng ta có thể lặp qua nó để trả về pool.
            Debug.Log($"[BombSpawnerManager] Clearing all {_instanceToPrefabMap.Count} active bombs.");
            // Tạo một bản sao của danh sách keys để tránh lỗi "Collection was modified" khi ReturnToPool sửa đổi nó.
            var activeInstances = new List<GameObject>(_instanceToPrefabMap.Keys);
            foreach (var bombInstance in activeInstances)
            {
                HandleBombDespawn(bombInstance); // SỬA LỖI: Gọi HandleBombDespawn để giảm bộ đếm active.
            }
        }

        protected override void OnGetInstance(GameObject instance)
        {
            base.OnGetInstance(instance);
            // Reset trạng thái của bom khi nó được lấy ra từ pool.
            if (instance.TryGetComponent<IBombController>(out var bombController))
            {
                bombController.ResetState();
            }

            // Tăng số lượng active cho loại prefab này
            if (_instanceToPrefabMap.TryGetValue(instance, out GameObject prefab))
            {
                _activeBombCounts[prefab] = _activeBombCounts.GetValueOrDefault(prefab, 0) + 1;
            }
        }

        private void HandleBombDespawn(GameObject instance)
        {
            if (_instanceToPrefabMap.TryGetValue(instance, out GameObject prefab))
                _activeBombCounts[prefab] = Mathf.Max(0, _activeBombCounts.GetValueOrDefault(prefab, 0) - 1);
            ReturnToPool(instance);
        }

        /// <summary>
        /// Đặt vị trí Y cho MẶT TRÊN của khu vực sinh bom.
        /// </summary>
        /// <param name="newTopY">Tọa độ Y mới cho mặt trên của khu vực sinh bom.</param>
        public void SetSpawnAreaTopY(float newTopY)
        {
            if (_spawnArea == null)
            {
                Debug.LogWarning("[BombSpawnerManager] Spawn Area (BoxCollider) is not assigned! Cannot set Y position.", this);
                return;
            }

            // Ta muốn mặt trên (world-space) của bounds của collider nằm ở newTopY.
            // Vị trí mặt trên = transform.position.y + center.y + (size.y / 2).
            // Từ đó, ta tính ra transform.position.y cần thiết.
            float newTransformY = newTopY - _spawnArea.center.y - (_spawnArea.size.y / 2f);

            // Lấy vị trí hiện tại và chỉ cập nhật thành phần Y.
            Vector3 newPosition = _spawnArea.transform.position;
            newPosition.y = newTransformY;
            _spawnArea.transform.position = newPosition;
        }

        /// <summary>
        /// Lấy một đối tượng bom từ pool.
        /// </summary>
        /// <returns>Một instance của GameObject từ pool.</returns>
        public GameObject GetBombFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return GetFromPool(prefab, position, rotation);
        }
    }
}