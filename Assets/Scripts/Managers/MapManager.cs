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
        [Tooltip("Đối tượng cha để chứa prefab map được tạo ra.")]
        [SerializeField] private GameObject _mapLoader;
        [Tooltip("Đối tượng cha để chứa dung nham được tạo ra.")]
        [SerializeField] private GameObject _lavaLoader;
        [Tooltip("Đối tượng cha để chứa các khối của thế giới ngầm.")]
        [SerializeField] private Transform _undergroundContainer;

        [Header("Generators")]
        [Tooltip("Số lượng đối tượng map được kích hoạt mỗi frame khi tải bất đồng bộ.")]
        [SerializeField] private int _mapObjectsPerFrame = 100;
        [Tooltip("Component chịu trách nhiệm sinh ra thế giới ngầm. Nếu bỏ trống, sẽ tự tìm trong scene.")]
        [SerializeField] private UndergroundGenerator _undergroundGenerator;

        [Header("Prefabs")]
        [Tooltip("Prefab của dung nham (lava).")]
        [SerializeField] private GameObject _lavaPrefab;

        // Instance của dung nham, được giữ lại giữa các round.
        private GameObject _lavaInstance;
        #endregion

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

            if (_undergroundGenerator == null)
            {
                _undergroundGenerator = FindAnyObjectByType<UndergroundGenerator>();
                if (_undergroundGenerator == null) Debug.LogWarning("[MapManager] UndergroundGenerator not found in scene. Underground will not be built.", this);
            }

            // Khởi tạo dung nham một lần duy nhất và tắt nó đi.
            if (_lavaPrefab != null && _lavaLoader != null && _lavaInstance == null)
            {
                _lavaInstance = Instantiate(_lavaPrefab, _lavaLoader.transform);
                _lavaInstance.SetActive(false);
            }
        }
        #endregion

        #region IMapManager Implementation
        /// <summary>
        /// Tải map và thế giới ngầm một cách bất đồng bộ (trong một coroutine).
        /// </summary>
        /// <returns>IEnumerator để có thể chạy như một coroutine.</returns>
        public IEnumerator LoadMapByIndexAsync(int mapIndex, int undergroundIndex)
        {
            yield return StartCoroutine(ClearCurrentMapAsync());

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

            yield return StartCoroutine(BuildMapAsync(mapPrefab, _mapObjectsPerFrame));
            yield return StartCoroutine(BuildUndergroundAsync(undergroundData));
            CreateLava();

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
            yield return StartCoroutine(ClearChildrenAsync(_mapLoader, 100));
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

            return mapData.MapPrefab.name;
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
            if (_mapLoader == null)
            {
                Debug.LogError("[MapManager] MapLoader container is not assigned. Cannot build map.", this);
                yield break;
            }

            // Bước 1: Instantiate prefab map chính. Thao tác này nhanh vì các con của nó sẽ bị tắt đi.
            GameObject mapInstance = Instantiate(mapPrefab, _mapLoader.transform);
            mapInstance.name = mapPrefab.name; // Dọn dẹp tên "(Clone)"

            // Bước 2: Lấy danh sách tất cả các object con trực tiếp và tắt chúng đi.
            var childrenToActivate = new List<Transform>();
            foreach (Transform child in mapInstance.transform)
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
            Debug.Log($"[MapManager] Activated lava: {_lavaInstance.name}");
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