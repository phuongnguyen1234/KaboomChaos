using UnityEngine;
using Core.Interfaces;
using Perks.Behaviors;

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
}