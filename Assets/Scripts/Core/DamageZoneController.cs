using UnityEngine;
using Core.Interfaces; // Cần để sử dụng interface IDamageable
using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Quản lý một vùng nguy hiểm (như dung nham, khí độc), gây sát thương theo thời gian cho bất kỳ đối tượng
    /// nào có interface IDamageable đi vào vùng ảnh hưởng.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DamageZoneController : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Lượng sát thương gây ra mỗi lần 'tick'.")]
        [SerializeField] private float _damageAmount = 25f;
        [Tooltip("Thời gian (giây) giữa mỗi lần gây sát thương.")]
        [SerializeField] private float _damageInterval = 0.3f;

        [Header("Status Effect Settings")]
        [Tooltip("Hiệu ứng sẽ được áp dụng cho các đối tượng trong vùng (ví dụ: Burning cho dung nham).")]
        [SerializeField] private StatusEffectType _effectToApply = StatusEffectType.Burning;
        [Tooltip("Thời gian hiệu ứng tồn tại (giây) sau khi một đối tượng thoát khỏi vùng.")]
        [SerializeField] private float _effectDurationOnExit = 5f;

        // Dictionary để theo dõi thời điểm tiếp theo mỗi đối tượng có thể nhận sát thương.
        private readonly Dictionary<IDamageable, float> _nextDamageTimestamps = new();

        private Collider _zoneTrigger;

        private void Awake()
        {
            _zoneTrigger = GetComponent<Collider>();
            // Đảm bảo collider được đặt làm trigger để các đối tượng có thể đi xuyên qua và phát hiện va chạm.
            if (!_zoneTrigger.isTrigger)
            {
                _zoneTrigger.isTrigger = true;
                Debug.LogWarning($"Collider trên '{gameObject.name}' chưa được đặt thành 'Is Trigger'. Script đã tự động thiết lập.", this);
            }
        }

        /// <summary>
        /// Được gọi bởi hệ thống vật lý cho mỗi collider tồn tại bên trong trigger.
        /// </summary>
        /// <param name="other">Collider đang ở bên trong trigger.</param>
        private void OnTriggerStay(Collider other)
        {
            // --- 1. Gây sát thương cho các đối tượng có thể nhận sát thương (như Player) ---
            // Logic này thường yêu cầu một marker như IPrimaryExplosionTarget để tránh gây sát thương cho các bộ phận phụ.
            if (other.GetComponent<IPrimaryExplosionTarget>() != null)
            {
                IDamageable damageable = other.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    // Kiểm tra xem đã đến lúc gây sát thương chưa.
                    if (_nextDamageTimestamps.TryGetValue(damageable, out float nextDamageTime))
                    {
                        if (Time.time >= nextDamageTime)
                        {
                            damageable.TakeDamage(_damageAmount, DamageSourceType.EnvironmentalContact);
                            _nextDamageTimestamps[damageable] = Time.time + _damageInterval;
                        }
                    }
                    else
                    {
                        // Nếu là lần đầu tiên, gây sát thương ngay và bắt đầu theo dõi.
                        damageable.TakeDamage(_damageAmount, DamageSourceType.EnvironmentalContact);
                        _nextDamageTimestamps.Add(damageable, Time.time + _damageInterval);
                    }
                }
            }

            // --- 2. Áp dụng hiệu ứng trạng thái cho các đối tượng có thể bị ảnh hưởng (như DestructiblePiece) ---
            // Logic này không cần marker và sẽ áp dụng cho bất kỳ đối tượng nào có StatusEffectReceiver.
            IStatusEffectable statusEffectable = other.GetComponentInParent<IStatusEffectable>();
            if (statusEffectable != null && _effectToApply != StatusEffectType.None)
            {
                // Liên tục "làm mới" hiệu ứng với một khoảng thời gian ngắn (lớn hơn một chút so với damage interval).
                // Điều này đảm bảo hiệu ứng sẽ duy trì miễn là đối tượng còn ở trong vùng.
                statusEffectable.ApplyStatusEffect(_effectToApply, _damageInterval * 2);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // --- 1. Dọn dẹp theo dõi sát thương ---
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null) { _nextDamageTimestamps.Remove(damageable); }

            // --- 2. Áp dụng hiệu ứng tồn tại sau khi thoát ---
            IStatusEffectable statusEffectable = other.GetComponentInParent<IStatusEffectable>();
            if (statusEffectable != null && _effectToApply != StatusEffectType.None)
            {
                // Áp dụng hiệu ứng lần cuối với thời gian tồn tại được định cấu hình.
                statusEffectable.ApplyStatusEffect(_effectToApply, _effectDurationOnExit);
            }
        }
    }
}