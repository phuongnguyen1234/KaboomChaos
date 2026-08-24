using UnityEngine;

namespace Collectibles.Data
{
    [CreateAssetMenu(fileName = "ShieldData_Crystal", menuName = "Kaboom Chaos/Collectibles/Crystal Shield")]
    public class CrystalShieldData : BaseShieldData
    {
        [Header("Crystal Shield Specifics")]
        [Tooltip("Ngưỡng sát thương mà khiên có thể hấp thụ. Sát thương lớn hơn hoặc bằng ngưỡng sẽ phá vỡ khiên.")]
        [SerializeField] private float _damageThreshold = 10f;
        public float DamageThreshold => _damageThreshold;

        [Header("Crystal Shield Breaking")]
        [Tooltip("VFX sẽ phát ra khi khiên vỡ.")]
        [SerializeField] private GameObject _breakVFX;
        public GameObject BreakVFX => _breakVFX;

        [Tooltip("SFX sẽ phát ra khi khiên vỡ.")]
        [SerializeField] private AudioClip _breakSFX;
        public AudioClip BreakSFX => _breakSFX;
    }
}