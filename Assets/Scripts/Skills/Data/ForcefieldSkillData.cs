using UnityEngine;
using Core.Interfaces;
using Skills.Behaviors;

namespace Skills.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Skill Forcefield.

    /// </summary>
    [CreateAssetMenu(fileName = "ForcefieldSkillData", menuName = "Skills/Forcefield")]
    public class ForcefieldSkillData : BaseSkillData
    {
        private ForcefieldBehavior _behavior;

        public override ISkillBehavior Behavior => _behavior ??= new ForcefieldBehavior(this);

        private void OnEnable()
        {
            _type = SkillType.Defensive;
        }
    }
}
