using Core.Interfaces;
using Collectibles.Data;

namespace Collectibles.Behaviors
{
    public class FireShieldBehavior : BaseShieldBehavior
    {
        public FireShieldBehavior(FireShieldData data) : base(data) { }

        /// <summary>
        /// Do uutien cao cho Fire Shield de kiem tra mien nhiem sat thuong lua/dung nham truoc khien vo.
        /// </summary>
        public override int Priority => 50;

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