using UnityEngine;
using Core.Interfaces;

namespace Perks.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Perk Speed Runner.
    /// Tang toc do di chuyen cua player.
    /// </summary>
    [CreateAssetMenu(fileName = "SpeedRunnerPerkData", menuName = "Kaboom Chaos/Perks/Speed Runner")]
    public class SpeedRunnerPerkData : BasePerkData
    {
        [Header("Thong So Speed Runner")]
        [Tooltip("He so nhan toc do di chuyen cua player (1.5 = tang 50%).")]
        [SerializeField] private float _moveSpeedMultiplier = 1.5f;
        public float MoveSpeedMultiplier => _moveSpeedMultiplier;

        private SpeedRunnerBehavior _behavior;

        public override IPerkBehavior Behavior => _behavior ??= new SpeedRunnerBehavior(this);
    }

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