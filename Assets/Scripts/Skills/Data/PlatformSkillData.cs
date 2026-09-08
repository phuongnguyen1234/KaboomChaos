using UnityEngine;
using Core.Interfaces;
using Skills.Behaviors;

namespace Skills.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Skill Platform: tao mot Platform duoi chan player.

    /// </summary>
    [CreateAssetMenu(fileName = "PlatformSkillData", menuName = "Skills/Platform")]
    public class PlatformSkillData : BaseSkillData
    {
        [Header("Platform Settings")]
        [Tooltip("Prefab cua Platform se duoc tao duoi chan player. Prefab nen co san component LifetimeController de tu dong xoa sau mot khoang thoi gian.")]
        [SerializeField] private GameObject _platformPrefab;

        [Tooltip("Khoang cach Y (offset) dieu chinh khoang cach spawn Platform so voi chan player. Gia tri duong: ben duoi chan;, gia tri am: ben tren chan.")]
        [SerializeField] private float _spawnYOffset = 0.2f;

        [Tooltip("Luc day/thoi len (momentum theo truc Y) dua player len cao mot chut truoc khi spawn Platform. Giup player khong bi roi xuong truoc khi Platform xuat hien.")]
        [SerializeField] private float _playerLiftImpulse = 6f;

        private PlatformBehavior _behavior;

        public GameObject PlatformPrefab => _platformPrefab;
        public float SpawnYOffset => _spawnYOffset;
        public float PlayerLiftImpulse => _playerLiftImpulse;

        public override ISkillBehavior Behavior => _behavior ??= new PlatformBehavior(this);

        private void OnEnable()
        {
            _type = SkillType.Movement;
        }
    }
}
