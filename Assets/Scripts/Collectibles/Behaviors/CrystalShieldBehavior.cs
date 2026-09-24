using Core.Interfaces;
using Collectibles.Data;
using UnityEngine;
using Core;

namespace Collectibles.Behaviors
{
    /// <summary>
    /// Hanh vi cua Crystal Shield: hap thu toan bo sat thuong duoi nguong (DamageThreshold)
    /// va vo khi nhan sat thuong lon hon hoac bang nguong.
    /// </summary>
    public class CrystalShieldBehavior : BaseShieldBehavior
    {
        #region Fields
        private CrystalShieldData _crystalData => _data as CrystalShieldData;
        private IPlayer _player;
        #endregion

        #region Properties
        /// <summary>
        /// Do uutien thap hon cac khien mien nhiem (Magic/Fire) de chi vo khi sat thuong khong duoc mien nhiem.
        /// </summary>
        public override int Priority => 10;
        #endregion

        #region Constructors
        public CrystalShieldBehavior(CrystalShieldData data) : base(data) {}
        #endregion

        #region Public Methods
        public override void OnApply(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data)
        {
            _player = player;
        }

        public override void OnFixedUpdate(IPlayer player, IBaseShieldData data) {}

        /// <summary>
        /// Don dep khi khien bi go bo (het han, reset round hoac bi vo).
        /// Khong phat SFX/VFX vo o day de tranh phat tieng vo khi reset round hoac tro ve lobby.
        /// </summary>
        public override void OnRemove(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data)
        {
            _player = null;
        }

        public override float OnDamageTaken(float damageAmount, DamageSourceType sourceType, StatusEffectType effectContext, IBaseShieldData data)
        {
            // Mien nhiem sat thuong tu khi doc
            if (effectContext == StatusEffectType.Poison)
            {
                return 0f;
            }

            // Mien nhiem sat thuong duoi nguong
            if (damageAmount < _crystalData.DamageThreshold)
            {
                return 0f; // Hap thu hoan toan
            }

            // Neu sat thuong >= nguong, khien hap thu don danh nay, phat hieu ung vo va tu pha vo.
            PlayBreakEffect(data as CrystalShieldData);
            return -1f; // Gia tri dac biet: hap thu va pha vo.
        }

        public override bool OnStatusEffectApplied(StatusEffectType effect, IBaseShieldData data)
        {
            // Chan hieu ung Poison
            return effect == StatusEffectType.Poison;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Phat hieu ung hat va am thanh khi khien pha le bi vo boi sat thuong.
        /// </summary>
        private void PlayBreakEffect(CrystalShieldData crystalData)
        {
            if (crystalData == null || _player?.GameObject == null) return;

            Vector3 playerPosition = _player.GameObject.transform.position;

            if (crystalData.BreakVFX != null)
            {
                GameEvents.TriggerVFXSpawnRequest(crystalData.BreakVFX, playerPosition, Quaternion.identity);
            }

            if (crystalData.BreakSFX != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(crystalData.BreakSFX, playerPosition);
            }
        }
        #endregion
    }
}

