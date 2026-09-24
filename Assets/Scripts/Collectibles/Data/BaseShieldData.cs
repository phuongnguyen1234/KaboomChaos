using UnityEngine;
using Core.Interfaces;

namespace Collectibles.Data
{
    /// <summary>
    /// Lớp cơ sở ScriptableObject cho tất cả các loại dữ liệu khiên.
    /// Chứa các thuộc tính chung của khiên.
    /// </summary>
    public abstract class BaseShieldData : ScriptableObject, IBaseShieldData
    {
        [Header("Shield Info")]
        [SerializeField] private string _effectName = "Generic Shield";
        public string EffectName => _effectName;

        [Tooltip("Thời gian hiệu lực của khiên (giây). 0 = tồn tại cho đến khi nhận sát thương hoặc hết round.")]
        [SerializeField] private float _duration = 10f;
        public float Duration => _duration;

        [Tooltip("Bật khi khiên này cần tồn tại vô thời hạn (không tự hết hạn theo Duration). Khi bật, khiên chỉ bị phá vỡ bởi sát thương hoặc khi round kết thúc.")]
        [SerializeField] private bool _unlimitedDuration = false;
        public bool UnlimitedDuration => _unlimitedDuration;

        [Tooltip("Prefab hiệu ứng hình ảnh sẽ được gắn vào người chơi khi khiên hoạt động.")]
        [SerializeField] private GameObject _shieldVFX;
        public GameObject ShieldVFX => _shieldVFX;

        [Tooltip("Màu sắc của hiệu ứng khiên (nếu VFX hỗ trợ).")]
        [SerializeField] private Color _shieldColor = Color.white;
        public Color ShieldColor => _shieldColor;
    }
}