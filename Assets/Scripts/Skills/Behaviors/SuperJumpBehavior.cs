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
            if (player == null || player.GameObject == null) return;

            // Kich hoat anh nion SuperJump tren player truoc khi ap dung luc bay len.
            var animBridge = player.GameObject.GetComponent<ISkillAnimationBridge>();
            if (animBridge != null) animBridge.TriggerSuperJumpAnimation();

            // Dat luc bay len theo truc dung Y (giu nguyen quan tinh ngang X, Z de luc nhay luon co dinh)
            player.SetVerticalMomentum(_data.JumpForce);
            Debug.Log($"[SuperJumpBehavior] Player {_data.DisplayName} da nhay cao voi luc: {_data.JumpForce}");
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