using Core.Interfaces;
using Collectibles.Data;
using UnityEngine;
using Core;

namespace Collectibles.Behaviors
{
    public class CrystalShieldBehavior : BaseShieldBehavior
    {
        private CrystalShieldData _crystalData => _data as CrystalShieldData;

        public CrystalShieldBehavior(CrystalShieldData data) : base(data) {}

        public override void OnApply(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data) {}

        public override void OnFixedUpdate(IPlayer player, IBaseShieldData data) {}

        public override void OnRemove(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data)
        {
            // Phát hiệu ứng vỡ khi bị gỡ bỏ (do hết hạn hoặc vỡ bởi sát thương)
            if (data is CrystalShieldData crystalData)
            {
                if (crystalData.BreakVFX != null)
                {
                    GameEvents.TriggerVFXSpawnRequest(crystalData.BreakVFX, player.GameObject.transform.position, Quaternion.identity);
                }
                var audioSource = player.GameObject.GetComponent<AudioSource>();
                if (audioSource != null && crystalData.BreakSFX != null)
                {
                    audioSource.PlayOneShot(crystalData.BreakSFX);
                }
            }
        }

        public override float OnDamageTaken(float damageAmount, DamageSourceType sourceType, StatusEffectType effectContext, IBaseShieldData data)
        {
            // Miễn nhiễm sát thương từ khí độc
            if (effectContext == StatusEffectType.Poison)
            {
                return 0f;
            }
            // Miễn nhiễm sát thương dưới ngưỡng
            if (damageAmount < _crystalData.DamageThreshold)
            {
                return 0f; // Hấp thụ hoàn toàn
            }

            // Nếu sát thương >= ngưỡng, khiên hấp thụ đòn đánh này và báo cho controller để tự phá vỡ.
            return -1f; // Giá trị đặc biệt: hấp thụ và phá vỡ.
        }

        public override bool OnStatusEffectApplied(StatusEffectType effect, IBaseShieldData data)
        {
            // Chặn hiệu ứng Poison
            return effect == StatusEffectType.Poison;
        }
    }
}