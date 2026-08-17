using Bombs.Data;
using Core.Interfaces;
using UnityEngine;

namespace Bombs.Explosions
{
    /// <summary>
    /// Xử lý vụ nổ của bom Matryoshka, sinh ra thế hệ tiếp theo.
    /// </summary>
    public class MatryoshkaExplosionStrategy : IExplosionStrategy
    {
        public void Execute(BombController controller)
        {
            if (controller.BombData is not MatryoshkaBombData matryoshkaData)
            {
                new DefaultExplosionStrategy().Execute(controller);
                return;
            }

            // 1. Vụ nổ của thế hệ hiện tại
            controller.TriggerSingleExplosion(controller.transform.position, false);
            controller.HandleSpecialInteractionsForExplosion(controller.transform.position);

            // 2. Kiểm tra và sinh ra thế hệ tiếp theo
            if (controller.CurrentGeneration < matryoshkaData.maxGenerations - 1)
            {
                GameObject childInstance = controller.BombSpawnerManager.GetBombFromPool(controller.BombData.BombPrefab, controller.transform.position, Quaternion.identity);
                if (childInstance != null && childInstance.TryGetComponent<BombController>(out var childController))
                {
                    childController.InitializeMatryoshka(controller.CurrentGeneration + 1);
                    childController.Activate();
                }
            }
        }
    }
}