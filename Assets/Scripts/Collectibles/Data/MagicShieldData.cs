using UnityEngine;

namespace Collectibles.Data
{
    [CreateAssetMenu(fileName = "ShieldData_Magic", menuName = "Kaboom Chaos/Collectibles/Magic Shield")]
    public class MagicShieldData : BaseShieldData
    {
        [Header("Magic Shield Specifics")]
        [Tooltip("Hệ số nhân tốc độ di chuyển khi khiên hoạt động.")]
        [SerializeField] private float _speedMultiplier = 1.5f;
        public float SpeedMultiplier => _speedMultiplier;
        [Tooltip("Hệ số nhân lực nhảy khi khiên hoạt động.")]
        [SerializeField] private float _jumpMultiplier = 1.2f;
        public float JumpMultiplier => _jumpMultiplier;

        [Header("Magic Shield Audio")]
        [Tooltip("Nhạc sẽ phát khi khiên này hoạt động.")]
        [SerializeField] private AudioClip _shieldMusic;
        public AudioClip ShieldMusic => _shieldMusic;
        [Header("Magic Shield Music")]
        [Tooltip("Pitch cơ bản của nhạc khiên (khi có 1 stack).")]
        [SerializeField] private float _basePitch = 1.0f;
        public float BasePitch => _basePitch;

        [Tooltip("Lượng pitch tăng thêm cho mỗi lần stack.")]
        [SerializeField] private float _pitchPerStack = 0.1f;
        public float PitchPerStack => _pitchPerStack;
    }
}