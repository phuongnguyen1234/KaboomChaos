using UnityEngine;
using Core.Interfaces;
using Skills.Behaviors;

namespace Skills.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Skill Super Jump.
    /// </summary>
    [CreateAssetMenu(fileName = "SuperJumpSkillData", menuName = "Skills/Super Jump")]
    public class SuperJumpSkillData : BaseSkillData
    {
        [Header("Thong So Super Jump")]
        [Tooltip("Luc nhay giup player bay len.")]
        [SerializeField] private float _jumpForce = 25f;
        public float JumpForce => _jumpForce;

        private SuperJumpBehavior _behavior;

        public override ISkillBehavior Behavior => _behavior ??= new SuperJumpBehavior(this);

        private void OnEnable()
        {
            _type = SkillType.Movement;
        }
    }
}
