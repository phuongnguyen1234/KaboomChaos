using UnityEngine;
using Core.Interfaces;
using System.Collections.Generic;
using Core;

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
        [Tooltip("Component chịu trách nhiệm sinh ra thế giới ngầm. Nếu bỏ trống, sẽ tự tìm trong scene.")]
        [SerializeField] private UndergroundGenerator _undergroundGenerator;

        [Header("Prefabs")]
        [Tooltip("Prefab của dung nham (lava).")]
        [SerializeField] private GameObject _lavaPrefab;
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
        }
        #endregion

        #region IMapManager Implementation
        /// <inheritdoc/>
        public void LoadMapByIndex(int mapIndex, int undergroundIndex)
        {
            ClearCurrentMap();

            // Kiểm tra tính hợp lệ của các chỉ số và database
            if (_mapDatabase == null || _mapDatabase.maps.Count == 0)
            {
                Debug.LogError("[MapManager] MapDatabase is not assigned or is empty.", this);
                return;
            }
            if (_undergroundDatabase == null || _undergroundDatabase.UndergroundDatas.Count == 0)
            {
                Debug.LogError("[MapManager] UndergroundDatabase is not assigned or is empty.", this);
                return;
            }
            if (mapIndex < 0 || mapIndex >= _mapDatabase.maps.Count)
            {
                Debug.LogError($"[MapManager] Invalid mapIndex: {mapIndex}. It's out of range for MapDatabase.", this);
                return;
            }
            if (undergroundIndex < 0 || undergroundIndex >= _undergroundDatabase.UndergroundDatas.Count)
            {
                Debug.LogError($"[MapManager] Invalid undergroundIndex: {undergroundIndex}. It's out of range for UndergroundDatabase.", this);
                return;
            }

            // Tải các thành phần
            var mapData = _mapDatabase.maps[mapIndex];
            if (mapData == null || mapData.MapPrefab == null)
            {
                Debug.LogError($"[MapManager] MapData at index {mapIndex} or its prefab is null.", this);
                return;
            }

            var mapPrefab = mapData.MapPrefab;
            var undergroundData = _undergroundDatabase.UndergroundDatas[undergroundIndex];

            BuildMap(mapPrefab);
            BuildUnderground(undergroundData);
            CreateLava();
        }

        /// <inheritdoc/>
        public void ClearCurrentMap()
        {
            Debug.Log("[MapManager] Clearing current map objects.");
            ClearChildren(_mapLoader);
            ClearChildren(_lavaLoader);
            if (_undergroundContainer != null) ClearChildren(_undergroundContainer.gameObject);
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
        #endregion

        #region Private Build Methods
        /// <summary>
        /// Sinh ra prefab map chính vào trong đối tượng MapLoader.
        /// </summary>
        private void BuildMap(GameObject mapPrefab)
        {
            if (mapPrefab == null)
            {
                Debug.LogError("[MapManager] mapPrefab is null. Cannot build map.", this);
                return;
            }
            if (_mapLoader == null)
            {
                Debug.LogError("[MapManager] MapLoader container is not assigned. Cannot build map.", this);
                return;
            }
            Instantiate(mapPrefab, _mapLoader.transform);
            Debug.Log($"[MapManager] Built map: {mapPrefab.name}");
        }

        /// <summary>
        /// Sinh ra dung nham vào trong đối tượng LavaLoader.
        /// </summary>
        private void CreateLava()
        {
            if (_lavaPrefab == null)
            {
                Debug.LogWarning("[MapManager] Lava Prefab is not assigned. Skipping lava creation.", this);
                return;
            }
            if (_lavaLoader == null)
            {
                Debug.LogError("[MapManager] LavaLoader container is not assigned. Cannot create lava.", this);
                return;
            }
            Instantiate(_lavaPrefab, _lavaLoader.transform);
            Debug.Log($"[MapManager] Created lava: {_lavaPrefab.name}");
        }

        /// <summary>
        /// Tạo các tầng địa chất dựa trên profile được cung cấp.
        /// Logic được chuyển từ UndergroundGenerator.cs.
        /// </summary>
        private void BuildUnderground(UndergroundData profile)
        {
            if (_undergroundGenerator == null)
            {
                Debug.LogError("[MapManager] UndergroundGenerator is not assigned and could not be found. Cannot build underground.", this);
                return;
            }
            _undergroundGenerator.Build(profile, _undergroundContainer);
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Xóa tất cả các đối tượng con của một đối tượng cha.
        /// </summary>
        private void ClearChildren(GameObject parent)
        {
            if (parent == null) return;
            foreach (Transform child in parent.transform)
            {
                Destroy(child.gameObject);
            }
        }
        #endregion
    }
}