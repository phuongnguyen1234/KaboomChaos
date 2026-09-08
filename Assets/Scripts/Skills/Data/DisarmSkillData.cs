using UnityEngine;
using Core.Interfaces;
using Skills.Behaviors;

namespace Skills.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Skill Disarm: doi het thoi gian duration (trong luc nay hien thi VFX charge quanh player),
    /// sau do go het bom trong ban kinh quanh player.
    /// </summary>
    [CreateAssetMenu(fileName = "DisarmSkillData", menuName = "Skills/Disarm")]
    public class DisarmSkillData : BaseSkillData
    {
        [Header("Thong So Disarm")]
        [Tooltip("Ban kinh (world) bom duoc go vo quanh player khi het duration.")]
        [SerializeField] private float _radius = 3f;

        [Header("VFX Charge")]
        [Tooltip("Prefab VFX particle hien thi khi dang 'charge' trong thoi gian duration. Duoc gan lam con cua player de di cung player.")]
        [SerializeField] private GameObject _chargeVfx;

        private DisarmBehavior _behavior;

        public float Radius => _radius;
        public GameObject ChargeVfx => _chargeVfx;

        public override ISkillBehavior Behavior => _behavior ??= new DisarmBehavior(this);

        private void OnEnable()
        {
            _type = SkillType.Defensive;
        }
    }
}