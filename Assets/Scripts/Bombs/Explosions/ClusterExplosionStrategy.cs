using Bombs.Data;
using Core.Interfaces;
using UnityEngine;

namespace Bombs.Explosions
{
    /// <summary>
    /// Xử lý vụ nổ của bom chùm, giải phóng các bom con.
    /// </summary>
    public class ClusterExplosionStrategy : IExplosionStrategy
    {
        public void Execute(BombController controller)
        {
            if (controller.BombData is not ClusterBombData clusterData || controller.BombSpawnerManager == null || clusterData.submunitionBombData == null)
            {
                // Fallback về hành vi nổ mặc định nếu dữ liệu không chính xác.
                new DefaultExplosionStrategy().Execute(controller);
                return;
            }

            // 1. Vụ nổ của vỏ bom chùm
            controller.TriggerSingleExplosion(controller.transform.position, false);
            controller.HandleSpecialInteractionsForExplosion(controller.transform.position);

            // 2. Sinh ra các bom con
            int count = Random.Range(clusterData.minSubmunitions, clusterData.maxSubmunitions + 1);
            for (int i = 0; i < count; i++)
            {
                // Lấy prefab cho bom con từ BombSpawnerManager
                GameObject submunitionPrefab = controller.BombSpawnerManager.GetPrefabForBombData(clusterData.submunitionBombData);
                if (submunitionPrefab == null)
                {
                    Debug.LogWarning($"[ClusterExplosionStrategy] Không tìm thấy prefab cho bom con '{clusterData.submunitionBombData.DisplayName}'. Bỏ qua.", controller);
                    continue;
                }

                Vector3 spawnPos = controller.transform.position + (Random.insideUnitSphere * clusterData.ejectionSpreadRadius);
                GameObject subInstance = controller.BombSpawnerManager.GetBombFromPool(submunitionPrefab, spawnPos, Quaternion.identity);

                if (subInstance != null)
                {
                    // Áp dụng lực đẩy
                    if (subInstance.TryGetComponent<Rigidbody>(out var subRb))
                    {
                        Vector3 ejectionDir = (spawnPos - controller.transform.position).normalized;
                        if (ejectionDir.sqrMagnitude < 0.001f) ejectionDir = Random.onUnitSphere;
                        subRb.AddForce(ejectionDir * clusterData.ejectionForce, ForceMode.Impulse);
                    }
                    // Khởi tạo và kích hoạt bom con
                    if (subInstance.TryGetComponent<IBombController>(out var subController))
                    {
                        // "Tiêm" dữ liệu và các manager cần thiết vào instance mới,
                        // lấy từ controller của quả bom mẹ.
                        subController.Initialize(clusterData.submunitionBombData, 
                                                 controller.BombSpawnerManager, 
                                                 controller.PlayerManager, 
                                                 controller.DestructionManager);
                        subController.Activate();
                    }
                }
            }
        }
    }
}