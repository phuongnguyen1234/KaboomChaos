using UnityEngine;
using Core.Interfaces;
using Skills.Data;

namespace Skills.Behaviors
{
    /// <summary>
    /// Hanh vi thuc thi cua Skill Forcefield: lam player bat tu trong thoi gian hieu luc (duration).
    /// </summary>
    public class ForcefieldBehavior : ISkillBehavior
    {
        private readonly ForcefieldSkillData _data;
        private bool _applied;
        private bool _wasInvincible;
        private IPlayer _player;

        public ForcefieldBehavior(ForcefieldSkillData data)
        {
            _data = data;
        }

        public void Activate(IPlayer player)
        {
            _player = player;
            _applied = false;
            if (player == null || player.GameObject == null) return;

            var invincible = player.GameObject.GetComponent<IInvincible>();
            if (invincible == null)
            {
                Debug.LogWarning("[ForcefieldBehavior] Player GameObject khong co component IInvincible!");
                return;
            }

            // Luu lai trang thai truoc do de khoi phuc khi het hieu luc.
            _wasInvincible = invincible.IsInvincible;

            invincible.IsInvincible = true;
            _applied = true;

            // Sat bool 'IsForcefieldOn' tren animator de chay anh nion Forcefield trong thoi gian hieu luc.
            SetForcefieldAnimation(player, true);
            Debug.Log("[ForcefieldBehavior] Player dang bat tu trong thoi gian hieu luc.");
        }

        /// <summary>
        /// Sat bool 'IsForcefieldOn' tren animator player: true khi skill dang hoat dong, false khi ket thuc.
        /// </summary>
        /// <param name="player">Nguoi choi dang su dung Forcefield skill.</param>
        /// <param name="active">True neu forcefield dang hoat dong.</param>
        private void SetForcefieldAnimation(IPlayer player, bool active)
        {
            if (player == null || player.GameObject == null) return;

            if (player.GameObject.TryGetComponent<ISkillAnimationBridge>(out var animBridge)) animBridge.SetForcefieldActive(active);
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Skill tuc thoi ve mat chi dinh trang thai; thoi gian duoc PlayerSkillController quan ly.
        }

        public void Deactivate(IPlayer player)
        {
            if (!_applied) return;

            var target = player ?? _player;
            var invincible = target?.GameObject?.GetComponent<IInvincible>();
            if (invincible != null)
            {
                invincible.IsInvincible = _wasInvincible;
                Debug.Log("[ForcefieldBehavior] Het hieu luc bat tu.");
            }

            // Sat bool 'IsForcefieldOn' sau khong lung (ket thuc hieu luc skill).
            SetForcefieldAnimation(target, false);

            _applied = false;
            _player = null;
        }
    }
}