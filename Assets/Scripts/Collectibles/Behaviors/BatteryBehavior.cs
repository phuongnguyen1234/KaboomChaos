using UnityEngine;
using Core;
using Core.Interfaces;
using Collectibles.Data;

namespace Collectibles.Behaviors
{
    /// <summary>
    /// Hanh vi cua collectible Battery: sac day energy khi player dang trong qua trinh tu sac (recharge).
    /// Neu energy dang day thi khong the nhap - hien thi thong bao 'Charge Full' tren UI (qua GameEvents).
    /// </summary>
    public class BatteryBehavior : ICollectibleBehavior
    {
        public bool Execute(ICollectibleController controller, IPlayer player)
        {
            if (controller.Data is not BatteryCollectibleData data) return false;
            if (player == null || player.GameObject == null) return false;

            var energy = player.GameObject.GetComponent<IEnergyable>();
            if (energy == null)
            {
                Debug.LogWarning("[BatteryBehavior] Player khong co component IEnergyable!", player.GameObject);
                return true;
            }

            // Neu energy dang day (100%) => khong the nhap: khong tieu thu vat pham.
            // Hien thi thong bao 'Charge Full' tren UI de nguoi choi biet.
            if (energy.IsFullyCharged)
            {
                Debug.Log("[BatteryBehavior] Energy dang day, khong the nhap Battery.", controller.GameObject);
                // Thong bao HUD: 'Charge Full' + SFX + pulse icon energy (HUDManager lang nghe event nay).
                GameEvents.TriggerBatteryCollectibleRefused(player, data.ChargeFullSfx);
                return false;
            }

            // Cas: chi sac day khi player dang trong qua trinh tu sac (recharge).
            // Neu khong dang sac thi khong ap dung.
            if (!energy.IsRecharging)
            {
                // Khong dang sac => khong tieu thu vat pham (tuong tu 'khong the nhap khi dang day').
                return false;
            }

            // Sac day 100% energy ngay lap tuc (RestoreFullEnergy da xu ly cancel qua trinh sac).
            float restored = energy.RestoreFullEnergy();
            if (restored > 0f)
            {
                Debug.Log($"[BatteryBehavior] Da sac day {restored} energy cho player.");
            }

            // Chay VFX thu thap neu co.
            if (data.CollectionVFX != null)
            {
                GameEvents.TriggerVFXSpawnRequest(data.CollectionVFX, controller.GameObject.transform.position, Quaternion.identity);
            }

            // Tieu thu vat pham (despawn).
            return true;
        }
    }
}