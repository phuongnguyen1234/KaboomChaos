using Core.Interfaces;
using Collectibles.Data;
using UnityEngine;

namespace Collectibles.Behaviors
{
    public class MagicShieldBehavior : BaseShieldBehavior
    {
        private MagicShieldData _magicData => _data as MagicShieldData;

        public MagicShieldBehavior(MagicShieldData data) : base(data) { }

        /// <summary>
        /// Music, movement/jump modifiers and pitch stacking are fully managed by
        /// PlayerShieldController. OnApply is intentionally a no-op so that stacking
        /// does not re-apply the speed/jump multipliers (only the music pitch stacks up).
        /// </summary>
        public override void OnApply(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data) { }

        public override void OnFixedUpdate(IPlayer player, IBaseShieldData data) { }

        /// <summary>
        /// Resume BGM and reset modifiers are handled inside PlayerShieldController.RemoveShield
        /// (only when no Magic Shield remains active).
        /// </summary>
        public override void OnRemove(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data) { }

        public override float OnDamageTaken(float damageAmount, DamageSourceType sourceType, StatusEffectType effectContext, IBaseShieldData data)
        {
            // Immune to all damage
            return 0f;
        }

        public override bool OnStatusEffectApplied(StatusEffectType effect, IBaseShieldData data)
        {
            // Immune to all status effects
            return true;
        }
    }
}