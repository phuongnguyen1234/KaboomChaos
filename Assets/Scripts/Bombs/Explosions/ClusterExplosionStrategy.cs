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
                Vector3 spawnPos = controller.transform.position + (Random.insideUnitSphere * clusterData.ejectionSpreadRadius);
                GameObject subInstance = controller.BombSpawnerManager.GetBombFromPool(clusterData.submunitionBombData.BombPrefab, spawnPos, Quaternion.identity);

                if (subInstance != null)
                {
                    if (subInstance.TryGetComponent<Rigidbody>(out var subRb))
                    {
                        Vector3 ejectionDir = (spawnPos - controller.transform.position).normalized;
                        if (ejectionDir.sqrMagnitude < 0.001f) ejectionDir = Random.onUnitSphere;
                        subRb.AddForce(ejectionDir * clusterData.ejectionForce, ForceMode.Impulse);
                    }

                    if (subInstance.TryGetComponent<IBombController>(out var subController))
                    {
                        subController.Activate();
                    }
                }
            }
        }
    }
}