using UnityEngine;
using System.Collections.Generic;
using Core;
using System.Collections;
using Core.Interfaces;

namespace Managers
{
    /// <summary>
    /// Chịu trách nhiệm tạo ra cấu trúc thế giới ngầm dựa trên một UndergroundData.
    /// Logic này được tách ra từ MapManager để tái sử dụng và quản lý dễ dàng hơn.
    /// </summary>
    public class UndergroundGenerator : MonoBehaviour, IUndergroundGenerator
    {
        public static IUndergroundGenerator Instance { get; private set; }

        /// <summary>
        /// Chiều cao (tọa độ Y) của điểm cao nhất của thế giới ngầm được tạo ra lần cuối.
        /// </summary>
        public float LastGeneratedHeight { get; private set; }

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

        /// <summary>
        /// Tạo các tầng địa chất một cách bất đồng bộ để tránh giật lag.
        /// </summary>
        /// <param name="profile">Dữ liệu cấu hình cho thế giới ngầm.</param>
        /// <param name="container">Đối tượng cha để chứa các khối.</param>
        /// <param name="blocksPerFrame">Số lượng khối được tạo mỗi frame.</param>
        /// <returns>IEnumerator để chạy như một coroutine.</returns>
        public IEnumerator BuildAsync(UndergroundData profile, Transform container, int blocksPerFrame = 50)
        {
            if (profile == null)
            {
                Debug.LogError("[UndergroundGenerator] UndergroundData is null. Cannot build.", this);
                yield break;
            }
            if (container == null)
            {
                Debug.LogError("[UndergroundGenerator] Container is null. Cannot build underground.", this);
                yield break;
            }
            if (profile.layers.Count == 0 || profile.layers[0].blockPrefab == null)
            {
                Debug.LogWarning("[UndergroundGenerator] UndergroundData has no layers or the first layer is missing a prefab. Cannot build.", this);
                yield break;
            }

            // Reset chiều cao trước khi xây dựng
            LastGeneratedHeight = 0;

            // --- Helper: Đo kích thước Prefab ---
            // Cache để lưu kích thước của các prefab đã được đo, tránh việc tạo và hủy đối tượng liên tục.
            var prefabSizes = new Dictionary<GameObject, Vector3>();
            Vector3 GetPrefabSize(GameObject prefab)
            {
                if (prefab == null) return Vector3.one;
                if (prefabSizes.TryGetValue(prefab, out Vector3 size)) return size;

                // Tạo một instance tạm thời để đo kích thước thực tế từ renderer.
                GameObject tempInstance = Instantiate(prefab);
                var renderer = tempInstance.GetComponentInChildren<Renderer>();
                Vector3 calculatedSize = Vector3.one;
                if (renderer != null)
                {
                    // renderer.bounds.size là kích thước thực tế của object trong world space.
                    calculatedSize = renderer.bounds.size;
                }
                else
                {
                    Debug.LogWarning($"[UndergroundGenerator] Prefab '{prefab.name}' không có Renderer để đo kích thước. Giả định kích thước là (1,1,1).", prefab);
                }
                Destroy(tempInstance);

                // Đảm bảo không có chiều nào bằng 0, tránh lỗi chia cho 0 hoặc grid bị dính vào nhau.
                if (calculatedSize.x <= 0) calculatedSize.x = 1;
                if (calculatedSize.y <= 0) calculatedSize.y = 1;
                if (calculatedSize.z <= 0) calculatedSize.z = 1;

                prefabSizes.Add(prefab, calculatedSize);
                return calculatedSize;
            }

            // --- Bước 1: Xác định kích thước chuẩn và tính toán offset ---
            // Giả định rằng kích thước của grid được quyết định bởi prefab của tầng đầu tiên.
            Vector3 standardBlockSize = GetPrefabSize(profile.layers[0].blockPrefab);
            
            // Tính toán offset để căn giữa grid, dựa trên kích thước thực tế của khối.
            float offsetX = (profile.gridSize.x - 1) * standardBlockSize.x / 2.0f;
            float offsetZ = (profile.gridSize.y - 1) * standardBlockSize.z / 2.0f;

            var random = new System.Random(profile.randomSeed);
            // Vị trí Y ban đầu (local) cho MẶT ĐÁY của khối thấp nhất trong container.
            float currentBottomY = 0.0f; // Dùng float để có vị trí chính xác.

            int blockCounter = 0;

            // --- Bước 2: Xây dựng các tầng từ dưới lên ---
            for (int i = profile.layers.Count - 1; i >= 0; i--)
            {
                var layer = profile.layers[i];
                if (layer.blockPrefab == null) continue;
                
                // Mỗi tầng có thể có chiều cao khối khác nhau.
                float layerBlockHeight = GetPrefabSize(layer.blockPrefab).y;

                for (int h = 0; h < layer.height; h++)
                {
                    for (int x = 0; x < profile.gridSize.x; x++)
                    {
                        for (int z = 0; z < profile.gridSize.y; z++)
                        {
                            // Xác định prefab và hiệu ứng sẽ được áp dụng
                            GameObject prefabToSpawn = layer.blockPrefab;
                            StatusEffectType effectToApply = layer.initialEffect;

                            if (layer.enableScattering && layer.scatterPrefab != null && (random.NextDouble() * 100.0 < layer.scatterPercentage))
                            {
                                prefabToSpawn = layer.scatterPrefab;
                                effectToApply = layer.scatterInitialEffect; // Sử dụng hiệu ứng của khối rải rác
                            }
                                

                            // Tính toán vị trí local dựa trên kích thước khối chuẩn của grid.
                            Vector3 localPos = new(
                                x * standardBlockSize.x - offsetX,
                                currentBottomY + (layerBlockHeight / 2f), // Pivot của khối ở giữa, nên đặt center của khối
                                z * standardBlockSize.z - offsetZ
                            );

                            // Tạo instance của prefab và đặt vị trí
                            GameObject blockInstance;
                            // Yêu cầu một khối từ pool thông qua hệ thống event.
                            blockInstance = GameEvents.TriggerBlockSpawnRequest(prefabToSpawn, Vector3.zero, Quaternion.identity);

                            if (blockInstance != null)
                            {
                                blockInstance.transform.SetParent(container);
                                blockInstance.transform.SetLocalPositionAndRotation(localPos, Quaternion.identity);
                            }
                            else
                            {
                                // Fallback: Nếu không có pool manager nào đang lắng nghe, tự tạo bằng Instantiate.
                                Debug.LogWarning($"[UndergroundGenerator] BlockPoolManager không hoạt động hoặc không thể sinh khối. Tự tạo instance cho '{prefabToSpawn.name}'. Hiệu năng sẽ bị ảnh hưởng.", this);
                                blockInstance = Instantiate(prefabToSpawn, container);
                                blockInstance.transform.SetLocalPositionAndRotation(localPos, Quaternion.identity);
                            }
                            blockInstance.name = $"{prefabToSpawn.name} ({x},{currentBottomY:F1},{z})";

                            // ÁP DỤNG HIỆU ỨNG BAN ĐẦU (NẾU CÓ)
                            if (effectToApply != StatusEffectType.None)
                            {
                                if (blockInstance.TryGetComponent<StatusEffectReceiver>(out var receiver))
                                {
                                    receiver.SetPermanentEffect(effectToApply);
                                }
                                else
                                {
                                    Debug.LogWarning($"Prefab '{prefabToSpawn.name}' được yêu cầu có hiệu ứng '{effectToApply}' nhưng không có component 'StatusEffectReceiver'.", blockInstance);
                                }
                            }
                            // Đảm bảo khối có component DestructibleBlock để có thể bị phá hủy
                            if (blockInstance.GetComponent<DestructibleBlock>() == null)
                                Debug.LogWarning($"Prefab '{prefabToSpawn.name}' không có component 'DestructibleBlock'. Sẽ không thể bị phá hủy.", blockInstance);

                            blockCounter++;
                            if (blockCounter >= blocksPerFrame)
                            {
                                blockCounter = 0;
                                yield return null; // Tạm dừng đến frame tiếp theo
                            }
                        }
                    }
                    // Tăng vị trí Y lên theo chiều cao của khối trong tầng này.
                    currentBottomY += layerBlockHeight;
                }
            }

            // Gán chiều cao cuối cùng để các hệ thống khác có thể sử dụng
            LastGeneratedHeight = currentBottomY; // LastGeneratedHeight là tọa độ Y của mặt trên cùng của khối cao nhất.
        }
    }
}
