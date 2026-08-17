using UnityEngine;
using Core.Interfaces;
using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho các vùng gây sát thương theo thời gian (AOE).
    /// Xử lý logic chung về việc theo dõi và gây sát thương cho các đối tượng IDamageable bên trong trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class BaseAreaDamager : MonoBehaviour
    {
        [Header("Area Damage Settings")]
        [Tooltip("Lượng sát thương gây ra mỗi lần 'tick'.")]
        [SerializeField] protected float _damageAmount = 5f;

        [Tooltip("Thời gian (giây) giữa mỗi lần gây sát thương.")]
        [SerializeField] protected float _damageInterval = 0.5f;

        [Tooltip("Layer của các đối tượng sẽ nhận sát thương.")]
        [SerializeField] protected LayerMask _damageableLayers;

        // Dictionary để theo dõi thời điểm tiếp theo mỗi đối tượng có thể nhận sát thương.
        protected readonly Dictionary<IDamageable, float> _nextDamageTimestamps = new();

        protected virtual void Awake()
        {
            var collider = GetComponent<Collider>();
            // Đảm bảo collider được đặt làm trigger để các đối tượng có thể đi xuyên qua và phát hiện va chạm.
            if (!collider.isTrigger)
            {
                collider.isTrigger = true;
                Debug.LogWarning($"Collider trên '{gameObject.name}' chưa được đặt thành 'Is Trigger'. Script đã tự động thiết lập.", this);
            }
        }

        protected virtual void OnEnable()
        {
            // Reset lại danh sách khi được tái sử dụng từ pool.
            _nextDamageTimestamps.Clear();
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            // Dọn dẹp đối tượng ra khỏi danh sách theo dõi khi nó thoát khỏi vùng.
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                _nextDamageTimestamps.Remove(damageable);
            }
        }

        /// <summary>
        /// Áp dụng sát thương lên một đối tượng, có kiểm tra thời gian chờ.
        /// </summary>
        /// <param name="damageable">Đối tượng để gây sát thương.</param>
        /// <param name="sourceType">Nguồn gốc của sát thương (ví dụ: môi trường, hiệu ứng).</param>
        /// <param name="effectContext">Ngữ cảnh hiệu ứng để truyền cho TakeDamage (ví dụ: Burning, Poison).</param>
        protected virtual void ApplyDamage(IDamageable damageable, DamageSourceType sourceType, StatusEffectType effectContext)
        {
            if (damageable == null) return;

            // Kiểm tra xem đã đến lúc gây sát thương chưa.
            if (!_nextDamageTimestamps.TryGetValue(damageable, out float nextDamageTime) || Time.time >= nextDamageTime)
            {
                damageable.TakeDamage(_damageAmount, sourceType, effectContext);
                _nextDamageTimestamps[damageable] = Time.time + _damageInterval;
            }
        }
    }
}