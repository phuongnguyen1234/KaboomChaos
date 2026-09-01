using UnityEngine;
using Core.Interfaces;

namespace Perks.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Perk High Jumper.
    /// Tang luc nhay cua player.
    /// </summary>
    [CreateAssetMenu(fileName = "HighJumperPerkData", menuName = "Kaboom Chaos/Perks/High Jumper")]
    public class HighJumperPerkData : BasePerkData
    {
        [Header("Thong So High Jumper")]
        [Tooltip("He so nhan luc nhay cua player (1.5 = tang 50%).")]
        [SerializeField] private float _jumpForceMultiplier = 1.5f;
        public float JumpForceMultiplier => _jumpForceMultiplier;

        private HighJumperBehavior _behavior;

        public override IPerkBehavior Behavior => _behavior ??= new HighJumperBehavior(this);
    }

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