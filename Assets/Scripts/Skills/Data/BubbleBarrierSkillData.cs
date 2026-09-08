using UnityEngine;
using Core.Interfaces;
using Skills.Behaviors;

namespace Skills.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Skill Bubble Barrier..
/// </summary>
    [CreateAssetMenu(fileName = "BubbleBarrierSkillData", menuName = "Skills/Bubble Barrier")]
    public class BubbleBarrierSkillData : BaseSkillData
    {
        [Header("Bubble Barrier Settings")]
        [Tooltip("Prefab cua Bubble Barrier. Prefab nay phai co san component BubbleBarrierController va LifetimeController.")]
        [SerializeField] private GameObject _bubblePrefab;

        private BubbleBarrierBehavior _behavior;

        public GameObject BubblePrefab => _bubblePrefab;

        public override ISkillBehavior Behavior => _behavior ??= new BubbleBarrierBehavior(this);

        private void OnEnable()
        {
            _type = SkillType.Defensive;
        }
    }
}