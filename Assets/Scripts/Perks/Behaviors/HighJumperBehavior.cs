using UnityEngine;
using Core.Interfaces;
using Perks.Data;

namespace Perks.Behaviors
{
    /// <summary>
    /// Hanh vi thuc thi cua Perk High Jumper.
    /// </summary>
    public class HighJumperBehavior : IPerkBehavior
    {
        private readonly HighJumperPerkData _data;

        public HighJumperBehavior(HighJumperPerkData data)
        {
            _data = data;
        }

        public void Apply(IPlayer player)
        {
            if (player != null)
            {
                player.ApplyJumpMultiplier(_data.JumpForceMultiplier);
                Debug.Log($"[HighJumperBehavior] Tang luc nhay cua player len x{_data.JumpForceMultiplier}");
            }
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Luc nhay duoc giu nguyen boi multiplier, khong can tick logic moi frame.
        }

        public void Remove(IPlayer player)
        {
            if (player != null)
            {
                player.RemoveJumpMultiplier(_data.JumpForceMultiplier);
                Debug.Log("[HighJumperBehavior] Da go bo he so luc nhay cua High Jumper.");
            }
        }
    }
}

