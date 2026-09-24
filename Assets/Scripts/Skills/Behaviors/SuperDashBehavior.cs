using UnityEngine;
using Core.Interfaces;
using Skills.Data;

namespace Skills.Behaviors
{
    /// <summary>
    /// Hanh vi thuc thi cua Skill Super Dash.
    /// </summary>
    public class SuperDashBehavior : ISkillBehavior
    {
        private readonly SuperDashSkillData _data;

        public SuperDashBehavior(SuperDashSkillData data)
        {
            _data = data;
        }

        public void Activate(IPlayer player)
        {
            if (player != null)
            {
                // Nhan toc do di chuyen cua player len
                player.ApplySpeedMultiplier(_data.SpeedMultiplier);
                Debug.Log($"[SuperDashBehavior] Tang toc do player len x{_data.SpeedMultiplier}");
            }
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Toc do tu dong duoc duy tri thong qua speed multiplier, khong can tick logic moi frame
        }

        public void Deactivate(IPlayer player)
        {
            if (player != null)
            {
                // Reset lai he so toc do
                player.RemoveSpeedMultiplier(_data.SpeedMultiplier);
                Debug.Log($"[SuperDashBehavior] Reset toc do player tro lai binh thuong");
            }
        }
    }
}