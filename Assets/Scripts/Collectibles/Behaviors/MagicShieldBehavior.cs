using Core.Interfaces;
using Collectibles.Data;
using UnityEngine;
using Core;

namespace Collectibles.Behaviors
{
    /// <summary>
    /// Hanh vi cua khien Magic Shield: mien nhiem toan bo sat thuong va hieu ung trang thai (bao gom dong bang).
    /// Tu dong ra dong player neu dang bi dong bang khi nhan khien.
    /// </summary>
    public class MagicShieldBehavior : BaseShieldBehavior
    {
        public MagicShieldBehavior(MagicShieldData data) : base(data) { }

        /// <summary>
        /// Do uutien cao nhat cho Magic Shield de mien nhiem hoan toan voi moi sat thuong.
        /// </summary>
        public override int Priority => 100;

        /// <summary>
        /// Khi nhan Magic Shield: ra dong ngay lap tuc neu player dang bi dong bang.
        /// Cac thuoc tinh ve toc do, nhay va nhac duoc quan ly trong PlayerShieldController.
        /// </summary>
        public override void OnApply(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data)
        {
            if (player?.GameObject != null)
            {
                var receiver = player.GameObject.GetComponent<StatusEffectReceiver>();
                if (receiver != null)
                {
                    receiver.UnfreezePlayer();
                }
            }
        }

        public override void OnFixedUpdate(IPlayer player, IBaseShieldData data) { }

        /// <summary>
        /// Resume BGM va reset modifiers duoc xu ly ben trong PlayerShieldController.RemoveShield
        /// (khi khong con Magic Shield nao hoat dong).
        /// </summary>
        public override void OnRemove(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data) { }

        public override float OnDamageTaken(float damageAmount, DamageSourceType sourceType, StatusEffectType effectContext, IBaseShieldData data)
        {
            // Mien nhiem moi loai sat thuong
            return 0f;
        }

        public override bool OnStatusEffectApplied(StatusEffectType effect, IBaseShieldData data)
        {
            // Mien nhiem tat ca cac hieu ung trang thai (bao gom ca Frozen, Burning, Electrified, Poison)
            return true;
        }
    }
}