using UnityEngine;
using Core.Interfaces;
using Perks.Data;

namespace Perks.Behaviors
{
    /// <summary>
    /// Hanh vi thuc thi cua Perk Speed Runner.
    /// </summary>
    public class SpeedRunnerBehavior : IPerkBehavior
    {
        private readonly SpeedRunnerPerkData _data;

        public SpeedRunnerBehavior(SpeedRunnerPerkData data)
        {
            _data = data;
        }

        public void Apply(IPlayer player)
        {
            if (player != null)
            {
                player.ApplySpeedMultiplier(_data.MoveSpeedMultiplier);
                Debug.Log($"[SpeedRunnerBehavior] Tang toc do di chuyen cua player len x{_data.MoveSpeedMultiplier}");
            }
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Toc do duoc giu nguyen boi multiplier, khong can tick logic moi frame.
        }

        public void Remove(IPlayer player)
        {
            if (player != null)
            {
                player.RemoveSpeedMultiplier(_data.MoveSpeedMultiplier);
                Debug.Log("[SpeedRunnerBehavior] Da go bo he so toc do cua Speed Runner.");
            }
        }
    }
}

