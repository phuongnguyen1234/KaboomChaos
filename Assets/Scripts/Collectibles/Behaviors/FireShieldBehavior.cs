using Core.Interfaces;
using Collectibles.Data;

namespace Collectibles.Behaviors
{
    public class FireShieldBehavior : BaseShieldBehavior
    {
        public FireShieldBehavior(FireShieldData data) : base(data) { }

        public override void OnApply(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data) { }

        public override void OnFixedUpdate(IPlayer player, IBaseShieldData data) { }

        public override void OnRemove(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data) { }

        public override float OnDamageTaken(float damageAmount, DamageSourceType sourceType, StatusEffectType effectContext, IBaseShieldData data)
        {
            // Miễn nhiễm sát thương từ nguồn Lửa (Burning) hoặc dung nham (EnvironmentalContact + Burning)
            if (effectContext == StatusEffectType.Burning)
            {
                return 0f; // Hấp thụ 100% sát thương lửa
            }
            // Khiên lửa chỉ miễn nhiễm với lửa, các loại sát thương khác sẽ đi xuyên qua.
            return damageAmount;
        }

        public override bool OnStatusEffectApplied(StatusEffectType effect, IBaseShieldData data)
        {
            // Chặn hiệu ứng Burning và Frozen
            return effect == StatusEffectType.Burning || effect == StatusEffectType.Frozen;
        }
    }
}