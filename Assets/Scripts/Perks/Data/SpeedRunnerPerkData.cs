using UnityEngine;
using Core.Interfaces;
using Perks.Behaviors;

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
}