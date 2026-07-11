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
        [Tooltip("Đối tượng cha để chứa các khối underground được tạo ra.")]
        [SerializeField] private GameObject _undergroundLoader;
        [Tooltip("Đối tượng cha để chứa prefab map được tạo ra.")]
        [SerializeField] private GameObject _mapLoader;
        [Tooltip("Đối tượng cha để chứa dung nham được tạo ra.")]
        [SerializeField] private GameObject _lavaLoader;

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
            if (_undergroundDatabase == null || _undergroundDatabase.undergroundProfiles.Count == 0)
            {
                Debug.LogError("[MapManager] UndergroundDatabase is not assigned or is empty.", this);
                return;
            }
            if (mapIndex < 0 || mapIndex >= _mapDatabase.maps.Count)
            {
                Debug.LogError($"[MapManager] Invalid mapIndex: {mapIndex}. It's out of range for MapDatabase.", this);
                return;
            }
            if (undergroundIndex < 0 || undergroundIndex >= _undergroundDatabase.undergroundProfiles.Count)
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
            var undergroundProfile = _undergroundDatabase.undergroundProfiles[undergroundIndex];

            BuildMap(mapPrefab);
            BuildUnderground(undergroundProfile);
            CreateLava();
        }

        /// <inheritdoc/>
        public void ClearCurrentMap()
        {
            Debug.Log("[MapManager] Clearing current map objects.");
            ClearChildren(_mapLoader);
            ClearChildren(_undergroundLoader);
            ClearChildren(_lavaLoader);
        }

        /// <inheritdoc/>
        public int GetMapCount()
        {
            // Trả về số lượng map nếu database tồn tại, ngược lại trả về 0.
            return _mapDatabase != null ? _mapDatabase.maps.Count : 0;
        }

        /// <inheritdoc/>
        public int GetUndergroundProfileCount()
        {
            // Trả về số lượng profile nếu database tồn tại, ngược lại trả về 0.
            return _undergroundDatabase != null ? _undergroundDatabase.undergroundProfiles.Count : 0;
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
        private void BuildUnderground(UndergroundProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError("[MapManager] UndergroundProfile is null. Cannot build underground.", this);
                return;
            }
            if (_undergroundLoader == null)
            {
                Debug.LogError("[MapManager] UndergroundLoader container is not assigned. Cannot build underground.", this);
                return;
            }

            Transform parent = _undergroundLoader.transform;
            var spawnedBlocks = new List<GameObject>();

            // Logic tạo underground được chuyển từ UndergroundGenerator
            Vector3 horizontalCellSize = Vector3.one;
            bool horizontalSizeDetermined = false;
            foreach (var layer in profile.layers)
            {
                if (layer.blockPrefab != null)
                {
                    horizontalCellSize = GetPrefabSize(layer.blockPrefab);
                    horizontalSizeDetermined = true;
                    break;
                }
            }
            if (!horizontalSizeDetermined)
            {
                Debug.LogWarning("[MapManager] Could not find a renderer on any prefab to determine grid size. Using default (1,1,1).", this);
            }

            var targetBlockData = new List<(Vector3 position, GameObject prefab, string name)>();
            var random = new System.Random(profile.randomSeed);

            float totalDisplacement = 0f;
            var layerBlockSizes = new List<Vector3>();
            foreach (var layer in profile.layers)
            {
                var size = GetPrefabSize(layer.blockPrefab);
                layerBlockSizes.Add(size);
                totalDisplacement += layer.height * (size.y + profile.spacing.y);
            }

            float currentY = -totalDisplacement;
            float offsetX = (profile.gridSize.x - 1) / 2.0f;
            float offsetZ = (profile.gridSize.y - 1) / 2.0f;

            for (int i = profile.layers.Count - 1; i >= 0; i--)
            {
                var layer = profile.layers[i];
                if (layer.blockPrefab == null) continue;

                Vector3 layerBlockSize = layerBlockSizes[i];

                for (int h = 0; h < layer.height; h++)
                {
                    float rowCenterY = currentY + profile.spacing.y + (layerBlockSize.y / 2f);

                    for (int x = 0; x < profile.gridSize.x; x++)
                    {
                        for (int z = 0; z < profile.gridSize.y; z++)
                        {
                            GameObject chosenPrefab = layer.blockPrefab;
                            if (layer.enableScattering && layer.scatterPrefab != null)
                            {
                                if (random.NextDouble() * 100.0 < layer.scatterPercentage)
                                    chosenPrefab = layer.scatterPrefab;
                            }

                            Vector3 position = new(
                                (x - offsetX) * (horizontalCellSize.x + profile.spacing.x),
                                rowCenterY,
                                (z - offsetZ) * (horizontalCellSize.z + profile.spacing.z)
                            );
                            string name = $"{chosenPrefab.name} ({x},{h},{z})";
                            targetBlockData.Add((position, chosenPrefab, name));
                        }
                    }
                    currentY += layerBlockSize.y + profile.spacing.y;
                }
            }

            for (int i = 0; i < targetBlockData.Count; i++)
            {
                var (position, prefab, name) = targetBlockData[i];
                var newBlock = Instantiate(prefab, parent);
                spawnedBlocks.Add(newBlock);
                newBlock.transform.localPosition = position;
                newBlock.name = name;
                newBlock.transform.SetSiblingIndex(i);
            }
            Debug.Log($"[MapManager] Built underground with {spawnedBlocks.Count} blocks.");
        }
        #endregion

        #region Utility Methods
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
                    Destroy(tempInstance);
            }
            return Vector3.one;
        }

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