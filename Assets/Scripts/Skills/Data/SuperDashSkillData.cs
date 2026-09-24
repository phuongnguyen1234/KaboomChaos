using UnityEngine;
using Core.Interfaces;
using Skills.Behaviors;

namespace Skills.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Skill Super Dash.
    /// </summary>
    [CreateAssetMenu(fileName = "SuperDashSkillData", menuName = "Skills/Super Dash")]
    public class SuperDashSkillData : BaseSkillData
    {
        [Header("Thong So Super Dash")]
        [Tooltip("He so nhan toc do chay cua player trong thoi gian dash.")]
        [SerializeField] private float _speedMultiplier = 2.0f;
        public float SpeedMultiplier => _speedMultiplier;

        private SuperDashBehavior _behavior;

        public override ISkillBehavior Behavior => _behavior ??= new SuperDashBehavior(this);

        private void OnEnable()
        {
            _type = SkillType.Movement;
        }
    }
}
