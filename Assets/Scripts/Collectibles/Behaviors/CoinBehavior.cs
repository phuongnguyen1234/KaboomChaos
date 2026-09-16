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

            // 1b. Thông báo sự kiện nhặt coin kèm BonusHP (đã cấu hình trong CoinData).
            // Perk Big Saver lắng nghe sự kiện này để hồi HP cho player.
            GameEvents.TriggerPlayerCoinCollected(player, data.BonusHP);

            // 2. Hiển thị floating text
            if (data.CreditValue > 0)
            {
                GameEvents.TriggerFloatingTextRequested(player.GameObject.transform, Vector3.up * 2f, $"+{data.CreditValue}", data.FloatingTextColor, player.CoinTextContainer, true);
            }

            return true;
        }
    }
}