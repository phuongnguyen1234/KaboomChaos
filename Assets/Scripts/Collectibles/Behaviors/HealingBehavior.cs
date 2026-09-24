using UnityEngine;
using Core.Interfaces;
using Core;
using Collectibles.Data;

namespace Collectibles.Behaviors
{
    public class HealingBehavior : ICollectibleBehavior
    {
        public bool Execute(ICollectibleController controller, IPlayer player)
        {
            if (controller.Data is not HealingCollectibleData data) return false;

            var healable = player.GameObject.GetComponent<IHealable>();
            var damageable = player.GameObject.GetComponent<IDamageable>();

            if (healable == null || damageable == null || !damageable.IsAlive) return false;

            // TÁI CẤU TRÚC LOGIC ĐỂ TRÁNH MỌI SỰ NHẬP NHẰNG
            // Bước 1: Tăng máu tối đa nếu vật phẩm có chỉ số này.
            if (data.BonusMaxHPAmount > 0)
            {
                healable.IncreaseMaxHealth(data.BonusMaxHPAmount);
            }

            // Bước 2: Hồi máu nếu vật phẩm có chỉ số này.
            // Logic này giờ hoàn toàn độc lập với việc tăng máu tối đa.
            if (data.HealAmount > 0)
            {
                float actualHealedAmount = healable.Heal(data.HealAmount);
                // Hiển thị floating text cho lượng máu thực tế đã hồi.
                if (actualHealedAmount > 0)
                {
                    GameEvents.TriggerFloatingTextRequested(player.GameObject.transform, Vector3.up * 2.2f, $"+{Mathf.RoundToInt(actualHealedAmount)}", data.FloatingTextColor, player.HPTextContainer, false);
                }
            }

            return true;
        }
    }
}