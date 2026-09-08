using UnityEngine;
using Core.Interfaces;
using Skills.Data;

namespace Skills.Behaviors
{
    /// <summary>
    /// Hanh vi thuc thi cua Skill Super Jump.
    /// </summary>
    public class SuperJumpBehavior : ISkillBehavior
    {
        private readonly SuperJumpSkillData _data;

        public SuperJumpBehavior(SuperJumpSkillData data)
        {
            _data = data;
        }

        public void Activate(IPlayer player)
        {
            if (player != null)
            {
                // Ap dung luc bay len theo truc dung Y
                player.AddMomentum(Vector3.up * _data.JumpForce);
                Debug.Log($"[SuperJumpBehavior] Player {_data.DisplayName} da nhay cao voi luc: {_data.JumpForce}");
            }
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Skill tuc thoi, khong can tick logic trong duration
        }

        public void Deactivate(IPlayer player)
        {
            Debug.Log("[SuperJumpBehavior] Super Jump ket thuc.");
        }
    }
}