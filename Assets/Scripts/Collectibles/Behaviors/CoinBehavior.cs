using UnityEngine;
using Core.Interfaces;
using Core;
using Collectibles.Data;

namespace Collectibles.Behaviors
{
    public class CoinBehavior : ICollectibleBehavior
    {
        public bool Execute(ICollectibleController controller, IPlayer player)
        {
            // Lấy dữ liệu từ controller và ép kiểu
            if (controller.Data is not CoinData data) return false;

            // 1. Cộng credit cho người chơi
            GameEvents.TriggerAddCreditsRequest(data.CreditValue);

            // 2. Hiển thị floating text
            if (data.CreditValue > 0)
            {
                GameEvents.TriggerFloatingTextRequested(player.GameObject.transform, Vector3.up * 2f, $"+{data.CreditValue}", data.FloatingTextColor, player.CoinTextContainer, true);
            }

            // 3. Phát VFX tại vị trí của vật phẩm
            if (data.CollectionVFX != null)
            {
                GameEvents.TriggerVFXSpawnRequest(data.CollectionVFX, controller.GameObject.transform.position, Quaternion.identity);
            }

            return true;
        }
    }
}