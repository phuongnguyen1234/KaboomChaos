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

            // SỬA LỖI: Lưu trữ dữ liệu và các manager của controller mẹ vào biến cục bộ.
            // Điều này để phòng trường hợp vụ nổ của chính controller (trong TriggerSingleExplosion)
            // gây ra hiệu ứng phụ làm thay đổi/reset trạng thái của controller mẹ trước khi
            // dữ liệu của nó được truyền cho con.
            // Dữ liệu được lấy từ `matryoshkaData` đã được cast an toàn.
            var parentData = matryoshkaData;
            var spawnerManager = controller.BombSpawnerManager;
            var playerManager = controller.PlayerManager;
            var destructionManager = controller.DestructionManager;

            // 1. Vụ nổ của thế hệ hiện tại
            controller.TriggerSingleExplosion(controller.transform.position, false);
            controller.HandleSpecialInteractionsForExplosion(controller.transform.position);

            // 2. Kiểm tra và sinh ra thế hệ tiếp theo
            // Sử dụng `parentData` (đã được cast) để truy cập `maxGenerations`.
            if (controller.CurrentGeneration < parentData.maxGenerations - 1)
            {
                if (spawnerManager == null)
                {
                    Debug.LogError("[MatryoshkaExplosionStrategy] BombSpawnerManager is null. Cannot spawn next generation.", controller);
                    return;
                }

                // Lấy prefab của chính quả bom này để sinh ra thế hệ tiếp theo.
                GameObject ownPrefab = spawnerManager.GetPrefabForInstance(controller.gameObject);
                if (ownPrefab == null)
                {
                    Debug.LogError("[MatryoshkaExplosionStrategy] Không thể lấy prefab cho instance hiện tại. Không thể sinh thế hệ tiếp theo.", controller);
                    return;
                }

                GameObject childInstance = spawnerManager.GetBombFromPool(ownPrefab, controller.transform.position, Quaternion.identity);
                if (childInstance != null && childInstance.TryGetComponent<BombController>(out var childController))
                {
                    // Quan trọng: "Tiêm" dữ liệu và các manager cần thiết vào instance mới,
                    // sử dụng các biến cục bộ đã được cache để đảm bảo chúng không bị null.
                    childController.Initialize(parentData,
                                               spawnerManager,
                                               playerManager,
                                               destructionManager);
                    childController.InitializeMatryoshka(controller.CurrentGeneration + 1);
                    childController.Activate();
                }
            }
        }
    }
}