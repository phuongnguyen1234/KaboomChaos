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
            Debug.Log("[ForcefieldBehavior] Player dang bat tu trong thoi gian hieu luc.");
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

            _applied = false;
            _player = null;
        }
    }
}