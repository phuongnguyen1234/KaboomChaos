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
        [Header("Collectible Settings")]
        [Tooltip("Database chứa tất cả các loại vật phẩm có thể thu thập.")]
        [SerializeField] private ScriptableObject _collectibleDatabaseAsset;
        [Tooltip("Đường cong xác suất (0-100) để một đợt vật phẩm được thả sau một đợt bom, dựa trên độ khó.")]
        [SerializeField] private AnimationCurve _collectibleWaveSpawnChance = AnimationCurve.Linear(1, 20, 6, 80);
        [Tooltip("Đường cong xác định số lượng vật phẩm TỐI THIỂU thả mỗi đợt (nếu đợt đó được kích hoạt), dựa trên độ khó.")]
        [SerializeField] private AnimationCurve _minCollectiblesPerWave = AnimationCurve.Linear(1, 1, 6, 1);
        [Tooltip("Đường cong xác định số lượng vật phẩm TỐI ĐA thả mỗi đợt (nếu đợt đó được kích hoạt), dựa trên độ khó.")]
        [SerializeField] private AnimationCurve _maxCollectiblesPerWave = AnimationCurve.Linear(1, 1, 6, 3);

        [Header("Spawning Settings")]
        [Tooltip("Đường cong xác định khoảng thời gian TỐI THIỂU (giây) giữa mỗi lần sinh bom, dựa trên độ khó. Trục X là độ khó, trục Y là thời gian.")]
        [SerializeField] private AnimationCurve _minSpawnIntervalByDifficulty = AnimationCurve.Linear(1, 2, 5, 0.5f);
        [Tooltip("Đường cong xác định khoảng thời gian TỐI ĐA (giây) giữa mỗi lần sinh bom, dựa trên độ khó. Trục X là độ khó, trục Y là thời gian.")]
        [SerializeField] private AnimationCurve _maxSpawnIntervalByDifficulty = AnimationCurve.Linear(1, 4, 5, 1.5f);
        [Tooltip("Đường cong xác định số lượng bom TỐI THIỂU thả mỗi lần, dựa trên độ khó. Trục X là độ khó, trục Y là số lượng.")]
        [SerializeField] private AnimationCurve _minBombsPerSpawnByDifficulty = AnimationCurve.Linear(1, 1, 5, 2);
        [Tooltip("Đường cong xác định số lượng bom TỐI ĐA thả mỗi lần, dựa trên độ khó. Trục X là độ khó, trục Y là số lượng.")]
        [SerializeField] private AnimationCurve _maxBombsPerSpawnByDifficulty = AnimationCurve.Linear(1, 1, 5, 5);

        [Header("Naval Mine Settings")]
        [Tooltip("Khoảng cách dọc (theo trục Y) mà thủy lôi được đặt lệch về phía trên so với điểm neo (anchor). Thủy lôi sẽ được đặt tại vị trí anchor + offset này để có thể trôi lên đúng cơ chế.")]
        [SerializeField] private float _navalMineAnchorSpawnOffset = 0.5f;
        [Tooltip("Khoảng cách NGANG (trên mặt phẳng XZ) tối thiểu giữa điểm neo của thủy lôi và chân người chơi. Giúp tránh việc mìn spawn ngay tại chân player rồi chạm và nổ tức thì (không công bằng). 0 = tắt kiểm tra này.")]
        [SerializeField] private float _navalMineMinPlayerHorizontalDistance = 1.5f;
        [Tooltip("Chiều cao (theo trục Y) của 'hành lang' phía trên điểm neo mà thủy lôi cần được thoáng (rỗng) để có thể trôi lên và kích hoạt. Nếu phía trên anchor bị khối chặn, mìn sẽ kẹt trong đất và không bao giờ arm được.")]
        [SerializeField] private float _navalMineClearanceHeight = 4f;
        [Tooltip("Nửa chiều rộng (trên mặt phẳng XZ) của 'hành lang' phía trên điểm neo mà thủy lôi yêu cầu được thoáng. Nên lớn hơn bán kính collider của mìn một chút.")]
        [SerializeField] private float _navalMineClearanceHalfWidth = 0.8f;

        // Buffer không-alloc tái sử dụng để kiểm tra khoảng trống phía trên anchor khi spawn thủy lôi.
        private readonly Collider[] _navalMineClearanceHits = new Collider[32];

        // Scene References (obtained from SceneObjectRegistry)
        private BoxCollider _spawnArea;

        private IDestructionManager _destructionManager; // Thêm trường cho DestructionManager
        private Coroutine _spawnCoroutine; // Coroutine cho việc sinh bom
        private IPlayerManager _playerManager; // Tham chiếu đến PlayerManager
        private IGameloopManager _gameloopManager; // Tham chiếu đến GameloopManager
        
        private IBombDatabase _bombDatabase; // Sử dụng interface cho BombDatabase
        private ICollectibleDatabase _collectibleDatabase;
        // Set để lưu trữ các bomb là biến thể, giúp tối ưu việc loại bỏ chúng khỏi danh sách spawn gốc.
        private readonly HashSet<IBaseBombData> _variantBombs = new(); // Sử dụng interface
        // Set để lưu trữ các vật phẩm là biến thể.
        private readonly HashSet<ICollectibleData> _variantCollectibles = new();

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
            _destructionManager = DestructionManager.Instance; // Lấy tham chiếu đến DestructionManager

            if (_bombDatabaseAsset is IBombDatabase bombDatabase)
            {
                _bombDatabase = bombDatabase;
            }
            else Debug.LogError("[BombSpawnerManager] Bomb Database Asset không phải là IBombDatabase hoặc chưa được gán.", this);

            if (_collectibleDatabaseAsset is ICollectibleDatabase collectibleDb)
            {
                _collectibleDatabase = collectibleDb;
            } // This is optional, so no error if it's null.

            PrewarmPools();
            PreprocessVariants();
            PreprocessCollectibleVariants();

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
            if (_bombDatabase == null || _bombDatabase.Mappings.Count == 0)
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

                // 3. Thả số lượng bom đã xác định
                for (int i = 0; i < bombsToSpawn; i++)
                {
                    IBombMapping selectedMapping = GetRandomBombMapping(waveSpawnCount);
                    if (selectedMapping?.Data == null || selectedMapping.Prefab == null)
                    {
                        Debug.LogWarning("[BombSpawnerManager] Could not select a valid bomb to spawn (check weights and limits for current difficulty). Skipping this bomb.");
                        continue; // Bỏ qua quả bom này, nhưng vẫn tiếp tục vòng lặp của đợt thả
                    }

                    IBaseBombData bombData = selectedMapping.Data;

                    GameObject bombInstance = GetFromPool(selectedMapping.Prefab, GetRandomSpawnPosition(bombData), Quaternion.identity);
                    if (bombInstance == null) continue;

                    // Cập nhật số lượng đã spawn trong đợt này
                    waveSpawnCount[bombData] = waveSpawnCount.GetValueOrDefault(bombData) + 1;

                    if (bombInstance.TryGetComponent<IBombController>(out var bombController))
                    {
                        bombController.Initialize(bombData, this, _playerManager, _destructionManager); // Truyền các Manager vào

                        // Thủy lôi (và các loại bom yêu cầu anchor) cần được gắn vào một điểm neo hợp lệ trên cấu trúc map.
                        if (bombController.RequiresAnchorForSpawn)
                        {
                            GameObject anchor = FindNearestValidAnchor(bombInstance.transform.position, _navalMineMinPlayerHorizontalDistance);
                            if (anchor == null)
                            {
                                // Không tìm thấy điểm neo hợp lệ -> không thể spawn thủy lôi. Hủy và trả về pool.
                                Debug.LogWarning("[BombSpawnerManager] Không tìm thấy điểm neo hợp lệ (DestructibleBlock hoặc DestructiblePart còn Intact) cho thủy lôi. Bỏ qua lần spawn này.", this);
                                waveSpawnCount[bombData] = Mathf.Max(0, waveSpawnCount[bombData] - 1);
                                GameEvents.TriggerBombDespawnRequest(bombInstance);
                                continue;
                            }

                            // Gán anchor cho hành vi thủy lôi thông qua BehaviorData.
                            bombController.BehaviorData = anchor;
                            // Đặt thủy lôi lệch lên phía trên anchor để nó có thể "trôi lên" đúng cơ chế.
                            bombInstance.transform.position = anchor.transform.position + Vector3.up * _navalMineAnchorSpawnOffset;
                        }

                        bombController.Activate();
                    }
                }

                // 4. Thử thả collectibles
                SpawnCollectibles(currentDifficulty);
            }
        }

        /// <summary>
        /// Khởi tạo và làm đầy sẵn các pool bom dựa trên BombDatabase.
        /// </summary>
        private void PrewarmPools()
        {
            if (_bombDatabase == null) return;

            foreach (var mapping in _bombDatabase.Mappings.Where(m => m.Prefab != null))
            {
                // Lấy ra và trả lại ngay lập tức để khởi tạo pool với số lượng ban đầu.
                var instances = new List<GameObject>();
                for (int i = 0; i < _initialPoolSize; i++)
                {
                    instances.Add(GetFromPool(mapping.Prefab, Vector3.zero, Quaternion.identity));
                }
                foreach (var instance in instances)
                {
                    HandleBombDespawn(instance); // SỬA LỖI: Gọi HandleBombDespawn để giảm bộ đếm active.
                }
            }
        }

        /// <summary>
        /// Thử sinh ra các vật phẩm thu thập dựa trên độ khó hiện tại.
        /// </summary>
        private void SpawnCollectibles(float currentDifficulty)
        {
            if (_collectibleDatabase == null || _collectibleDatabase.Mappings.Count == 0) return;

            // --- LOGIC MỚI: Quyết định xem có nên thả đợt vật phẩm này không ---
            if (Random.Range(0f, 100f) >= _collectibleWaveSpawnChance.Evaluate(currentDifficulty))
            {
                return; // Không may mắn, không có vật phẩm nào được thả lần này.
            }

            // --- LOGIC MỚI: Xác định số lượng vật phẩm sẽ thả trong đợt này ---
            int minCollectibles = Mathf.RoundToInt(_minCollectiblesPerWave.Evaluate(currentDifficulty));
            int maxCollectibles = Mathf.RoundToInt(_maxCollectiblesPerWave.Evaluate(currentDifficulty));
            if (minCollectibles > maxCollectibles) minCollectibles = maxCollectibles;
            int collectiblesToSpawn = Random.Range(minCollectibles, maxCollectibles + 1);

            if (collectiblesToSpawn <= 0) return;

            Debug.Log($"[BombSpawnerManager] Spawning {collectiblesToSpawn} collectible(s).");

            // --- LOGIC MỚI: Chọn và thả vật phẩm dựa trên trọng số, tương tự như bom ---
            for (int i = 0; i < collectiblesToSpawn; i++)
            {
                // Lấy một mapping vật phẩm ngẫu nhiên dựa trên trọng số (Rarity)
                ICollectibleMapping selectedMapping = GetRandomCollectibleMapping(currentDifficulty);
                if (selectedMapping != null)
                {
                    SpawnSingleCollectible(selectedMapping);
                }
            }
        }

        /// <summary>
        /// Sinh ra một instance của một vật phẩm cụ thể tại một vị trí ngẫu nhiên.
        /// </summary>
        private void SpawnSingleCollectible(ICollectibleMapping mapping)
        {
            if (mapping?.Data == null || mapping.Prefab == null) return;

            // Lấy vị trí spawn ngẫu nhiên.
            Bounds bounds = _spawnArea.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 spawnPosition = new(randomX, bounds.max.y, randomZ);
            
            // Sử dụng Prefab từ mapping để spawn
            GameObject collectibleInstance = GameEvents.TriggerCollectibleSpawnRequest(mapping.Prefab, spawnPosition, Quaternion.identity);
            if (collectibleInstance != null && collectibleInstance.TryGetComponent<ICollectibleController>(out var controller))
            {
                controller.Initialize(mapping.Data); // "Tiêm" Data từ mapping
            }
        }

        /// <summary>
        /// Chon mot loai vat pham ngau nhien tu database dua tren trong so.
        /// Neu vat pham goc duoc chon co khai bao bien the ma khong bien the nao qua duoc
        /// phep tung xuc xac theo do kho thi tra ve null (khong fallback ve vat pham goc,
        /// coi nhu khong spawn duoc vat pham do trong luot nay).
        /// </summary>
        private ICollectibleMapping GetRandomCollectibleMapping(float currentDifficulty)
        {
            // --- Bước 1: Chọn một vật phẩm cơ bản dựa trên trọng số ---
            var weightedList = new List<(ICollectibleMapping mapping, float weight)>();
            float totalWeight = 0f;

            foreach (var mapping in _collectibleDatabase.Mappings)
            {
                if (mapping?.Data == null || mapping.Prefab == null) continue;

                // Bỏ qua các vật phẩm được định nghĩa là biến thể của một vật phẩm khác.
                if (_variantCollectibles.Contains(mapping.Data)) continue;

                float weight = mapping.Data.RarityByDifficulty.Evaluate(currentDifficulty);
                if (weight > 0)
                {
                    weightedList.Add((mapping, weight));
                    totalWeight += weight;
                }
            }

            if (totalWeight == 0) return null;

            // Chọn một mapping vật phẩm gốc
            float randomValue = Random.Range(0, totalWeight);
            ICollectibleMapping selectedMapping = null;
            foreach (var (mapping, weight) in weightedList)
            {
                if (randomValue < weight)
                {
                    selectedMapping = mapping;
                    break;
                }
                randomValue -= weight;
            }

            if (selectedMapping == null) return null; // Fallback

            // --- Bước 2: Kiểm tra xem có nên thay thế bằng một trong các biến thể không ---
            bool hasVariants = selectedMapping.Data.PossibleVariants != null && selectedMapping.Data.PossibleVariants.Count > 0;
            if (hasVariants)
            {
                foreach (var variantInfo in selectedMapping.Data.PossibleVariants)
                {
                    if (variantInfo?.VariantCollectibleData == null) continue;

                    // Tap trung viec tra cuu variant -> mapping de log warning ro rang khi thieu mapping.
                    var variantMapping = GetMappingForCollectibleData(variantInfo.VariantCollectibleData);
                    if (variantMapping?.Prefab == null)
                    {
                        // Neu bien the da cau hinh khong co mapping/prefab hop le thi no khong bao gio duoc spawn.
                        // Nguyen nhan pho bien: asset bien the chua duoc them vao danh sach Mappings cua CollectibleDatabase.
                        Debug.LogWarning($"[BombSpawnerManager] Collectible variant '{variantInfo.VariantCollectibleData.DisplayName}' is not registered in the CollectibleDatabase mappings. Add its 'CollectibleMapping' entry so it can appear.", this);
                        continue;
                    }

                    float variantChance = variantInfo.SpawnChanceByDifficulty.Evaluate(currentDifficulty);
                    if (Random.Range(0f, 100f) < variantChance)
                    {
                        return variantMapping; // Tra ve bien the va dung lai.
                    }
                }

                // Khong fallback ve vat pham goc: neu vat pham goc co khai bao bien the ma
                // khong bien the nao tung xuc xac thanh cong thi coi nhu khong spawn duoc
                // vat pham nay trong luot nay (caller se bo qua slot nay khi nhan null).
                return null;
            }

            // Vat pham goc khong khai bao bien the nao, tra ve chinh no.
            return selectedMapping;
        }

        private IBombMapping GetRandomBombMapping(Dictionary<IBaseBombData, int> waveSpawnCount)
        {
            float currentDifficulty = _gameloopManager.CurrentIntensity;
            // --- Bước 1: Chọn một loại bom cơ bản dựa trên trọng số ---
            var weightedList = new List<(IBombMapping mapping, float weight)>();
            float totalWeight = 0f;

            foreach (var mapping in _bombDatabase.Mappings)
            {
                var bombData = mapping.Data;

                // Bỏ qua các prefab null và các bomb được định nghĩa là biến thể của một bomb khác.
                // Điều này đảm bảo chúng không được chọn một cách độc lập.
                if (mapping.Prefab == null || bombData == null) continue;
                if (_variantBombs.Contains(bombData)) continue;

                // --- KIỂM TRA GIỚI HẠN MỚI ---
                // 1. Kiểm tra giới hạn số lượng active toàn cục
                if (bombData.MaxActiveInstances > 0 && GetActiveCount(mapping.Prefab) >= bombData.MaxActiveInstances)
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
                    weightedList.Add((mapping, weight));
                    totalWeight += weight;
                }
            }

            if (totalWeight == 0) return null;

            // Chọn một mapping bom gốc
            float randomValue = Random.Range(0, totalWeight);
            IBombMapping selectedMapping = null;
            foreach (var (mapping, weight) in weightedList)
            {
                if (randomValue < weight)
                {
                    selectedMapping = mapping;
                    break;
                }
                randomValue -= weight;
            }

            if (selectedMapping == null) return null; // Fallback

            // --- Bước 2: Kiểm tra xem có nên thay thế bằng một trong các biến thể (variant) không ---
            // Duyệt qua danh sách các biến thể có thể có.
            if (selectedMapping.Data.PossibleVariants != null && selectedMapping.Data.PossibleVariants.Count > 0)
            {
                foreach (var variantInfo in selectedMapping.Data.PossibleVariants)
                {
                    // Tìm mapping tương ứng cho variant data
                    var variantMapping = _bombDatabase.Mappings.FirstOrDefault(m => m.Data == variantInfo.VariantBombData);

                    if (variantInfo?.VariantBombData == null || variantMapping?.Prefab == null)
                    {
                        continue; // Bỏ qua biến thể không hợp lệ
                    }

                    var variantData = variantInfo.VariantBombData;

                    // --- KIỂM TRA GIỚI HẠN MỚI CHO BIẾN THỂ ---
                    // 1. Kiểm tra giới hạn số lượng active toàn cục
                    if (variantData.MaxActiveInstances > 0 && GetActiveCount(variantMapping.Prefab) >= variantData.MaxActiveInstances)
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
                        return variantMapping;
                    }
                }
            }

            // Nếu không có biến thể nào được chọn, trả về bom gốc.
            return selectedMapping;
        }

        /// <summary>
        /// Đếm số lượng instance đang hoạt động của một loại bom cụ thể.
        /// </summary>
        /// <param name="prefab">Prefab của loại bom cần đếm.</param>
        /// <returns>Số lượng instance đang hoạt động.</returns>
        private int GetActiveCount(GameObject prefab)
        {
            if (prefab == null) return 0;
            return _activeBombCounts.GetValueOrDefault(prefab, 0);
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
            if (_bombDatabase == null || _bombDatabase.Mappings == null) return;

            foreach (var mapping in _bombDatabase.Mappings)
            {
                if (mapping.Data != null && mapping.Data.PossibleVariants != null)
                {
                    foreach (var variant in mapping.Data.PossibleVariants)
                    {
                        if (variant != null && variant.VariantBombData != null)
                            _variantBombs.Add(variant.VariantBombData);
                    }
                }
            }
        }

        /// <summary>
        /// Xây dựng một danh sách các vật phẩm là biến thể để loại bỏ chúng khỏi vòng quay spawn gốc.
        /// </summary>
        private void PreprocessCollectibleVariants()
        {
            _variantCollectibles.Clear();
            if (_collectibleDatabase == null || _collectibleDatabase.Mappings == null) return;

            foreach (var mapping in _collectibleDatabase.Mappings)
            {
                if (mapping.Data != null && mapping.Data.PossibleVariants != null)
                {
                    foreach (var variant in mapping.Data.PossibleVariants)
                    {
                        if (variant != null && variant.VariantCollectibleData != null)
                            _variantCollectibles.Add(variant.VariantCollectibleData);
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
        /// Tìm điểm neo hợp lệ gần nhất cho thủy lôi (Naval Mine): một <see cref="DestructibleBlock"/>
        /// hoặc một <see cref="DestructiblePart"/> còn nguyên vẹn (<see cref="PartState.Intact"/>) đang active trong scene.
        /// Ưu tiên anchor gần vị trí tham chiếu (vốn đã được lệch về phía người chơi), nhưng LOẠI TRỪ các anchor
        /// nằm trong bán kính ngang quanh chân người chơi để tránh mìn nổ ngay tại chân player.
        /// </summary>
        /// <param name="fromPosition">Vị trí tham chiếu để chọn anchor gần nhất.</param>
        /// <param name="minPlayerHorizontalDist">Khoảng cách ngang tối thiểu với chân người chơi (0 = không kiểm tra).</param>
        /// <returns>GameObject của anchor hợp lệ, hoặc null nếu không tìm thấy.</returns>
        private GameObject FindNearestValidAnchor(Vector3 fromPosition, float minPlayerHorizontalDist)
        {
            // Gom tất cả anchor hợp lệ kèm khoảng cách đến vị trí tham chiếu.
            var candidates = new List<(DestructibleBlock block, DestructiblePart part, float dist)>();
            float sqrMinPlayerDist = minPlayerHorizontalDist * minPlayerHorizontalDist;

            foreach (var block in FindObjectsByType<DestructibleBlock>(FindObjectsInactive.Exclude))
            {
                if (block == null || block.gameObject == null || !block.gameObject.activeInHierarchy) continue;
                float dist = Vector3.Distance(block.transform.position, fromPosition);
                candidates.Add((block, null, dist));
            }

            foreach (var part in FindObjectsByType<DestructiblePart>(FindObjectsInactive.Exclude))
            {
                if (part == null || part.gameObject == null || !part.gameObject.activeInHierarchy) continue;
                if (part.CurrentState != PartState.Intact) continue; // Chỉ neo vào các mảnh còn nguyên vẹn
                float dist = Vector3.Distance(part.transform.position, fromPosition);
                candidates.Add((null, part, dist));
            }

            if (candidates.Count == 0) return null;

            // Thu thập vị trí chân của các người chơi đang trong round (chỉ so sánh khoảng cách ngang XZ).
            List<Vector3> playerFeet = null;
            if (sqrMinPlayerDist > 0f && _playerManager != null)
            {
                var players = _playerManager.GetPlayersInRound();
                var playersToUse = players != null && players.Count > 0 ? players : _playerManager.GetAllPlayers();
                if (playersToUse != null && playersToUse.Count > 0)
                {
                    playerFeet = new List<Vector3>(playersToUse.Count);
                    foreach (var player in playersToUse)
                    {
                        if (player != null && player.GameObject != null)
                            playerFeet.Add(player.GameObject.transform.position);
                    }
                }
            }

            bool IsNearAnyPlayerFeet(Vector3 position)
            {
                if (playerFeet == null || playerFeet.Count == 0) return false;
                foreach (var feet in playerFeet)
                {
                    Vector2 delta = new Vector2(position.x - feet.x, position.z - feet.z);
                    if (delta.sqrMagnitude < sqrMinPlayerDist) return true;
                }
                return false;
            }

            // Sắp xếp theo khoảng cách tăng dần để ưu tiên anchor gần fromPosition (vốn gần player) nhất.
            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            // Chọn anchor đầu tiên thỏa mãn CẢ HAI điều kiện:
            // 1. Không nằm trong vùng chân của bất kỳ người chơi nào.
            // 2. Khoảng trống PHÍA TRÊN anchor đủ rộng và thoáng để mìn có thể trôi lên và kích hoạt (arm).
            foreach (var c in candidates)
            {
                Vector3 candidatePos = c.block != null ? c.block.transform.position : c.part.transform.position;
                if (IsNearAnyPlayerFeet(candidatePos)) continue;
                if (!HasClearSpaceAbove(candidatePos)) continue;
                return c.block != null ? c.block.gameObject : c.part.gameObject;
            }

            // Không có anchor nào vừa thoáng phía trên vừa cách chân player -> bỏ qua spawn.
            // Nếu ép chọn anchor bị bao quanh, mìn sẽ kẹt trong đất, không bao giờ arm được (vô hình).
            return null;
        }

        /// <summary>
        /// Kiểm tra xem "hành lang" phía trên vị trí đã cho có thoáng (không bị khối chặn) hay không.
        /// Một thủy lôi chỉ có thể trôi lên và kích hoạt nếu không gian phía trên điểm neo của nó trống rỗng.
        /// </summary>
        /// <param name="anchorPosition">Vị trí điểm neo (world) cần kiểm tra phía trên.</param>
        /// <returns>True nếu phía trên thoáng, False nếu bị khối chặn.</returns>
        private bool HasClearSpaceAbove(Vector3 anchorPosition)
        {
            if (_navalMineClearanceHeight <= 0f) return true; // Tắt kiểm tra này nếu chiều cao = 0

            float halfHeight = _navalMineClearanceHeight * 0.5f;
            float halfWidth = Mathf.Max(_navalMineClearanceHalfWidth, 0.01f);

            // Tâm box kiểm tra nằm phía trên anchor (từ ngay trên anchor đến đỉnh hành lang).
            Vector3 center = anchorPosition + Vector3.up * (halfHeight + _navalMineAnchorSpawnOffset);
            Vector3 halfExtents = new Vector3(halfWidth, halfHeight, halfWidth);

            int hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, _navalMineClearanceHits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = _navalMineClearanceHits[i];
                if (hit == null || hit.gameObject == null) continue;

                // Chỉ coi là vật cản nếu là khối phá hủy được hoặc mảnh vỡ còn nguyên vẹn trong khu vực thoáng.
                if (hit.GetComponent<DestructibleBlock>() != null) return false;
                if (hit.TryGetComponent<DestructiblePart>(out var part) && part.CurrentState == PartState.Intact) return false;
            }
            return true;
        }

        /// <summary>
        /// Lấy một đối tượng bom từ pool.
        /// </summary>
        /// <returns>Một instance của GameObject từ pool.</returns>
        public GameObject GetBombFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return GetFromPool(prefab, position, rotation);
        }

        /// <summary>
        /// Lấy prefab tương ứng với một dữ liệu bom cụ thể từ database.
        /// </summary>
        /// <param name="data">Dữ liệu bom cần tìm prefab.</param>
        /// <returns>Prefab của bom, hoặc null nếu không tìm thấy.</returns>
        public GameObject GetPrefabForBombData(IBaseBombData data)
        {
            if (_bombDatabase == null || data == null) return null;
            return _bombDatabase.Mappings.FirstOrDefault(m => m.Data == data)?.Prefab;
        }

        /// <summary>
        /// Returns the collectible mapping (data + prefab) for the given collectible data.
        /// </summary>
        /// <param name="data">The collectible data (e.g. a variant) to resolve a mapping for.</param>
        /// <returns>The matching mapping, or null if no data/prefab entry is found.</returns>
        private ICollectibleMapping GetMappingForCollectibleData(ICollectibleData data)
        {
            if (_collectibleDatabase == null || data == null) return null;
            return _collectibleDatabase.Mappings.FirstOrDefault(m => m.Data == data);
        }

        /// <summary>
        /// Lấy prefab gốc của một instance bom đang hoạt động từ map nội bộ.
        /// </summary>
        /// <param name="instance">Instance của bom.</param>
        /// <returns>Prefab gốc, hoặc null nếu không tìm thấy.</returns>
        public GameObject GetPrefabForInstance(GameObject instance)
        {
            if (instance == null) return null;
            _instanceToPrefabMap.TryGetValue(instance, out var prefab);
            return prefab;
        }
    }
}