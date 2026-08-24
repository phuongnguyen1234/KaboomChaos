using UnityEngine;
using Core.Interfaces;
using Collectibles.Data;

namespace Collectibles.Behaviors
{
    public class ShieldBehavior : ICollectibleBehavior
    {
        public bool Execute(ICollectibleController controller, IPlayer player)
        {
            if (controller.Data is not ShieldCollectibleData shieldCollectibleData) return false; // Lỗi biên dịch đã được sửa bằng cách thêm 'Data' vào ICollectibleController

            IBaseShieldData shieldData = shieldCollectibleData.ShieldData;
            if (shieldData == null || shieldData as Object == null)
            {
                Debug.LogWarning("GiveShieldBehavior is missing a reference to a shield data to apply.", controller.GameObject);
                return false;
            }

            var shieldController = player.GameObject.GetComponentInChildren<IPlayerShieldController>();
            if (shieldController != null)
            {
                // Yêu cầu: Không thể nhặt Crystal Shield nếu đã có 1 cái.
                // Trả về false để KHÔNG tiêu thụ vật phẩm → node được giữ lại, nhặt được sau khi hết khiên.
                if (shieldData is CrystalShieldData && shieldController.IsShieldTypeActive(typeof(CrystalShieldData)))
                {
                    Debug.Log("[ShieldBehavior] Đã có Crystal Shield, bỏ qua vật phẩm và giữ nguyên node.", controller.GameObject);
                    return false;
                }
                
                // Logic âm thanh đã được tái cấu trúc và chuyển đi nơi khác.
                // - SFX nhặt vật phẩm giờ được xử lý bởi CollectibleController.
                // - Nhạc của Magic Shield được xử lý bởi PlayerShieldController.
                shieldController.ApplyShield(shieldData);
                return true;
            }

            Debug.LogWarning($"Player {player.GameObject.name} does not have a PlayerShieldController to receive a shield.", player.GameObject);
            return true;
        }
    }
}