using UnityEngine;
using Core.Interfaces;
using System.Collections.Generic;
using Core;
using System.Collections;

namespace Managers
{
    /// <summary>
    /// Quản lý việc tải, tạo và dọn dẹp các thành phần của một màn chơi,
    /// bao gồm map chính, thế giới ngầm (underground) và dung nham (lava).
    /// </summary>
    public class MapManager : MonoBehaviour, IMapManager
    {
        #region Singleton
        /// <summary>
        /// Thể hiện Singleton của MapManager, cho phép truy cập toàn cục.
        /// </summary>
        public static IMapManager Instance { get; private set; }
        #endregion

        #region Fields
        [Header("Databases")]
        [Tooltip("Database chứa các cấu hình thế giới ngầm.")]
        [SerializeField] private UndergroundDatabase _undergroundDatabase;
        [Tooltip("Database chứa các prefab map.")]
        [SerializeField] private MapDatabase _mapDatabase;

        [Header("Scene Containers")]
        [Tooltip("Số lượng đối tượng map được kích hoạt mỗi frame khi tải bất đồng bộ.")]
        [SerializeField] private int _mapObjectsPerFrame = 100;

        [Header("Prefabs")]
        [Tooltip("Prefab của dung nham (lava).")]
        [SerializeField] private GameObject _lavaPrefab;

        // Instance của dung nham, được giữ lại giữa các round.
        private GameObject _mapInstance; // Lưu trữ instance của map đã được sinh ra
        private GameObject _lavaInstance;

        // Scene References (obtained from SceneObjectRegistry)
        private GameObject _mapContainer;
        private GameObject _lavaContainer;
        private Transform _undergroundContainer;
        private IUndergroundGenerator _undergroundGenerator;
        #endregion

        /// <summary>
        /// Tọa độ Y cao nhất của map hiện tại (bao gồm cả underground và map chính).
        /// Các đối tượng như TopBorder, BombSpawner, PlayerSpawn có thể sử dụng giá trị này để đặt vị trí.
        /// </summary>
        public float MapTopY { get; private set; }

        #region Unity Lifecycle
        private void Awake()
        {
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
        #endregion

        private void Start()
        {
            // Lấy tham chiếu đến các đối tượng trong scene từ Registry
            var registry = SceneObjectRegistry.Instance;
            if (registry != null)
            {
                _mapContainer = registry.MapContainer;
                _lavaContainer = registry.LavaContainer;
                _undergroundContainer = registry.UndergroundContainer;
            }
            else Debug.LogError("[MapManager] SceneObjectRegistry.Instance is null!", this);

            // Lấy tham chiếu đến manager trong Start() để đảm bảo nó đã được Bootstrapper khởi tạo.
            _undergroundGenerator = UndergroundGenerator.Instance;
            if (_undergroundGenerator == null) Debug.LogWarning("[MapManager] UndergroundGenerator.Instance is null. Underground will not be built.", this);

            // Khởi tạo dung nham một lần duy nhất và tắt nó đi.
            // Phải thực hiện sau khi đã lấy được _lavaContainer từ Registry.
            if (_lavaPrefab != null && _lavaContainer != null && _lavaInstance == null)
            {
                _lavaInstance = Instantiate(_lavaPrefab, _lavaContainer.transform);
                _lavaInstance.SetActive(false);
            }
        }

        #region IMapManager Implementation
        /// <summary>
        /// Tải map và thế giới ngầm một cách bất đồng bộ (trong một coroutine).
        /// </summary>
        /// <returns>IEnumerator để có thể chạy như một coroutine.</returns>
        public IEnumerator LoadMapByIndexAsync(int mapIndex, int undergroundIndex)
        {
            yield return StartCoroutine(ClearCurrentMapAsync());
            _mapInstance = null; // Đảm bảo _mapInstance là null trước khi tải map mới

            // Kiểm tra tính hợp lệ của các chỉ số và database
            if (_mapDatabase == null || _mapDatabase.maps.Count == 0)
            {
                Debug.LogError("[MapManager] MapDatabase is not assigned or is empty.", this);
                yield break;
            }
            if (_undergroundDatabase == null || _undergroundDatabase.UndergroundDatas.Count == 0)
            {
                Debug.LogError("[MapManager] UndergroundDatabase is not assigned or is empty.", this);
                yield break;
            }
            if (mapIndex < 0 || mapIndex >= _mapDatabase.maps.Count)
            {
                Debug.LogError($"[MapManager] Invalid mapIndex: {mapIndex}. It's out of range for MapDatabase.", this);
                yield break;
            }
            if (undergroundIndex < 0 || undergroundIndex >= _undergroundDatabase.UndergroundDatas.Count)
            {
                Debug.LogError($"[MapManager] Invalid undergroundIndex: {undergroundIndex}. It's out of range for UndergroundDatabase.", this);
                yield break;
            }

            // Tải các thành phần
            var mapData = _mapDatabase.maps[mapIndex];
            if (mapData == null || mapData.MapPrefab == null)
            {
                Debug.LogError($"[MapManager] MapData at index {mapIndex} or its prefab is null.", this);
                yield break;
            }

            var mapPrefab = mapData.MapPrefab;
            var undergroundData = _undergroundDatabase.UndergroundDatas[undergroundIndex];

            // --- THAY ĐỔI LOGIC: Xây dựng thế giới ngầm TRƯỚC để lấy chiều cao của nó ---
            // 1. Xây dựng thế giới ngầm.
            yield return StartCoroutine(BuildUndergroundAsync(undergroundData));

            // 2. Lấy chiều cao của thế giới ngầm và đặt vị trí cho map chính.
            // Điều này đảm bảo map chính sẽ nằm ngay trên mặt của thế giới ngầm.
            if (_undergroundGenerator != null && _mapContainer != null)
            {   // LastGeneratedHeight là chiều cao LOCAL của underground so với _undergroundContainer.
                // undergroundTopY phải là chiều cao WORLD của mặt trên cùng của underground.
                float undergroundTopY = _undergroundContainer.position.y + _undergroundGenerator.LastGeneratedHeight;
                _mapContainer.transform.position = new Vector3(0, undergroundTopY, 0);
            }

            // 3. Xây dựng map chính ở vị trí đã được điều chỉnh và lưu trữ instance.
            yield return StartCoroutine(BuildMapAsync(mapPrefab, _mapObjectsPerFrame));
            // 4. Tạo dung nham.
            CreateLava();

            // 5. Tính toán và lưu trữ chiều cao tổng thể của map (bao gồm cả underground và map chính)
            if (_mapContainer != null && _mapInstance != null)
            {
                // Lấy chiều cao cục bộ của bề mặt map (từ pivot của _mapInstance).
                float mapSurfaceLocalY = GetMapSurfaceLocalY(_mapInstance);
                MapTopY = _mapContainer.transform.position.y + mapSurfaceLocalY; // MapTopY là world Y của mặt trên của map chính.
            }
            // Tạm thời yield null một frame để đảm bảo việc build hoàn tất trước khi sang bước tiếp theo
            yield return null;
        }

        /// <summary>
        /// Dọn dẹp map hiện tại một cách bất đồng bộ.
        /// </summary>
        public IEnumerator ClearCurrentMapAsync()
        {
            Debug.Log("[MapManager] Clearing current map objects asynchronously.");
            if (_lavaInstance != null) _lavaInstance.SetActive(false);

            // Reset vị trí của map container về gốc để chuẩn bị cho lần tải map tiếp theo.
            if (_mapContainer != null)
            {
                _mapContainer.transform.position = Vector3.zero;
                _mapInstance = null; // Xóa tham chiếu đến instance map cũ
            }

            yield return StartCoroutine(ClearChildrenAsync(_mapContainer, 100));
            // Các khối underground được quản lý bởi pool, nên chúng ta sẽ trả chúng về pool thay vì hủy.
            if (_undergroundContainer != null) yield return StartCoroutine(ReturnAllBlocksToPoolAsync(_undergroundContainer.gameObject, 200));
        }

        /// <inheritdoc/>
        public int GetMapCount()
        {
            // Trả về số lượng map nếu database tồn tại, ngược lại trả về 0.
            return _mapDatabase != null ? _mapDatabase.maps.Count : 0;
        }

        /// <inheritdoc/>
        public int GetUndergroundDataCount()
        {
            // Trả về số lượng profile nếu database tồn tại, ngược lại trả về 0.
            return _undergroundDatabase != null ? _undergroundDatabase.UndergroundDatas.Count : 0;
        }

        /// <summary>
        /// Lấy tên của một map dựa trên chỉ số (index) của nó trong database.
        /// </summary>
        /// <param name="index">Chỉ số của map trong MapDatabase.</param>
        /// <returns>Tên của prefab map, hoặc một chuỗi báo lỗi nếu không tìm thấy.</returns>
        public string GetMapNameByIndex(int index)
        {
            if (_mapDatabase == null || _mapDatabase.maps == null || index < 0 || index >= _mapDatabase.maps.Count)
            {
                Debug.LogWarning($"[MapManager] Invalid map index for GetMapNameByIndex: {index}");
                return "Unknown Map";
            }

            var mapData = _mapDatabase.maps[index];
            if (mapData == null || mapData.MapPrefab == null)
            {
                Debug.LogWarning($"[MapManager] MapData or its prefab is null at index: {index}");
                return "Invalid Map";
            }

            return mapData.MapName;
        }
        #endregion

        #region Private Build Methods
        /// <summary>
        /// Sinh ra prefab map chính vào trong đối tượng MapLoader một cách bất đồng bộ.
        /// Kích hoạt các đối tượng con của map theo từng cụm để tránh giật lag.
        /// </summary>
        private IEnumerator BuildMapAsync(GameObject mapPrefab, int objectsPerFrame)
        {
            if (mapPrefab == null)
            {
                Debug.LogError("[MapManager] mapPrefab is null. Cannot build map.", this);
                yield break;
            }
            if (_mapContainer == null)
            {
                Debug.LogError("[MapManager] MapLoader container is not assigned. Cannot build map.", this);
                yield break;
            }
            // Bước 1: Instantiate prefab map chính. Thao tác này nhanh vì các con của nó sẽ bị tắt đi.
            _mapInstance = Instantiate(mapPrefab, _mapContainer.transform); // Gán vào trường _mapInstance
            _mapInstance.name = mapPrefab.name; // Dọn dẹp tên "(Clone)" cho trường _mapInstance

            // Bước 2: Lấy danh sách tất cả các object con trực tiếp và tắt chúng đi.
            var childrenToActivate = new List<Transform>();
            foreach (Transform child in _mapInstance.transform)
            {
                childrenToActivate.Add(child);
                child.gameObject.SetActive(false);
            }

            Debug.Log($"[MapManager] Building map '{mapPrefab.name}' asynchronously with {childrenToActivate.Count} objects.");

            // Bước 3: Kích hoạt lại các object con theo từng cụm mỗi frame.
            for (int i = 0; i < childrenToActivate.Count; i++)
            {
                if (childrenToActivate[i] != null)
                {
                    childrenToActivate[i].gameObject.SetActive(true);
                }

                if ((i + 1) % objectsPerFrame == 0)
                {
                    yield return null; // Đợi đến frame tiếp theo
                }
            }
            Debug.Log($"[MapManager] Finished building map: {mapPrefab.name}");
        }

        /// <summary>
        /// Sinh ra dung nham vào trong đối tượng LavaLoader.
        /// </summary>
        private void CreateLava()
        {
            if (_lavaInstance == null)
            {
                Debug.LogWarning("[MapManager] Lava Prefab is not assigned. Skipping lava creation.", this);
                return;
            }
            _lavaInstance.SetActive(true);
        }

        /// <summary>
        /// Tạo các tầng địa chất dựa trên profile được cung cấp.
        /// Logic được chuyển từ UndergroundGenerator.cs.
        /// </summary>
        private IEnumerator BuildUndergroundAsync(UndergroundData profile)
        {
            if (_undergroundGenerator == null)
            {
                Debug.LogError("[MapManager] UndergroundGenerator is not assigned and could not be found. Cannot build underground.", this);
                yield break;
            }
            yield return StartCoroutine(_undergroundGenerator.BuildAsync(profile, _undergroundContainer));
        }
        #endregion
        
        /// <summary>
        /// Lấy tọa độ Y cục bộ của bề mặt map. Ưu tiên tìm một BoxCollider trigger trên root của mapInstance.
        /// Nếu không tìm thấy BoxCollider trên root của mapInstance, sẽ trả về 0 và ghi log lỗi.
        /// </summary>
        /// <param name="instantiatedMap">GameObject của map đã được sinh ra.</param>
        /// <returns>Tọa độ Y cục bộ của bề mặt map (tính từ pivot của instantiatedMap).</returns>
        private float GetMapSurfaceLocalY(GameObject instantiatedMap)
        {
            if (instantiatedMap == null) return 0f;
        
            // Tìm BoxCollider trên root của mapInstance
            if (instantiatedMap.TryGetComponent<BoxCollider>(out var mapTopCollider))
            {
                // Trả về tọa độ Y cục bộ của mặt trên của collider.
                // mapTopCollider.center là local position của tâm collider.
                // mapTopCollider.size.y là chiều cao local của collider.
                return mapTopCollider.center.y + mapTopCollider.size.y / 2f;
            }
        
            Debug.LogError($"[MapManager] Map prefab '{instantiatedMap.name}' does not have a BoxCollider on its root. Cannot determine MapTopY. Returning 0.", instantiatedMap);
            return 0f;
        }
        #region Utility Methods

        /// <summary>
        /// Trả tất cả các khối con của một đối tượng cha về lại Block Pool.
        /// </summary>
        private IEnumerator ReturnAllBlocksToPoolAsync(GameObject parent, int objectsPerFrame = 200)
        {
            if (parent == null) yield break;

            // Kiểm tra xem có pool manager nào đang lắng nghe event despawn không.
            if (!GameEvents.IsBlockPoolListening())
            {
                Debug.LogWarning("[MapManager] BlockPoolManager not found or not listening. Falling back to destroying underground blocks.", this);
                yield return StartCoroutine(ClearChildrenAsync(parent, objectsPerFrame));
                yield break;
            }
            var childrenToReturn = new List<Transform>();
            foreach (Transform child in parent.transform)
            {
                childrenToReturn.Add(child);
            }

            for (int i = 0; i < childrenToReturn.Count; i++)
            {
                if (childrenToReturn[i] != null)
                {
                    // Gửi yêu cầu trả về pool thông qua hệ thống event.
                    GameEvents.TriggerBlockDespawnRequest(childrenToReturn[i].gameObject);
                }
                if ((i + 1) % objectsPerFrame == 0)
                {
                    yield return null; // Đợi đến frame tiếp theo
                }
            }
        }

        /// <summary>
        /// Xóa tất cả các đối tượng con của một đối tượng cha.
        /// </summary>
        private IEnumerator ClearChildrenAsync(GameObject parent, int objectsPerFrame = 100)
        {
            if (parent == null) yield break;

            // Tạo một danh sách các con để hủy, vì không thể sửa đổi collection khi đang duyệt qua nó.
            var childrenToDestroy = new List<Transform>();
            foreach (Transform child in parent.transform)
            {
                childrenToDestroy.Add(child);
            }

            for (int i = 0; i < childrenToDestroy.Count; i++)
            {
                if (childrenToDestroy[i] != null)
                {
                    Destroy(childrenToDestroy[i].gameObject);
                }
                if ((i + 1) % objectsPerFrame == 0)
                {
                    yield return null; // Đợi đến frame tiếp theo
                }
            }
        }
        #endregion
    }
}