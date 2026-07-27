using UnityEngine;
using Core.Interfaces; // Cần để sử dụng interface IDamageable
using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Quản lý hành vi của dung nham, gây sát thương theo thời gian cho bất kỳ đối tượng
    /// nào có interface IDamageable đi vào vùng ảnh hưởng.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class LavaController : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Lượng sát thương gây ra mỗi giây cho các đối tượng bên trong dung nham.")]
        [SerializeField] private float _damagePerSecond = 25f;

        // Dictionary để theo dõi thời điểm tiếp theo mỗi đối tượng có thể nhận sát thương.
        private readonly Dictionary<IDamageable, float> _nextDamageTimestamps = new();

        private BoxCollider _lavaTrigger;

        private void Awake()
        {
            _lavaTrigger = GetComponent<BoxCollider>();
            // Đảm bảo collider được đặt làm trigger để các đối tượng có thể đi xuyên qua.
            if (!_lavaTrigger.isTrigger)
            {
                _lavaTrigger.isTrigger = true;
                Debug.LogWarning($"BoxCollider trên '{gameObject.name}' chưa được đặt thành 'Is Trigger'. Script đã tự động thiết lập.", this);
            }
        }

        /// <summary>
        /// Được gọi bởi hệ thống vật lý cho mỗi collider tồn tại bên trong trigger.
        /// </summary>
        /// <param name="other">Collider đang ở bên trong trigger.</param>
        private void OnTriggerStay(Collider other)
        {
            // Chỉ xét CapsuleCollider của người chơi để tránh các trigger phụ.
            if (other is not CapsuleCollider) return;

            // Tìm một component có thể nhận sát thương trên đối tượng hoặc cha của nó.
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) return;

            // Kiểm tra xem đối tượng đã được theo dõi chưa.
            if (_nextDamageTimestamps.TryGetValue(damageable, out float nextDamageTime))
            {
                // Nếu đã đến lúc, gây sát thương và cập nhật mốc thời gian tiếp theo.
                if (Time.time >= nextDamageTime)
                {
                    damageable.TakeDamage(_damagePerSecond);
                    _nextDamageTimestamps[damageable] = Time.time + 1f;
                }
            }
            else
            {
                // Nếu là lần đầu tiên phát hiện, gây sát thương ngay lập tức và bắt đầu theo dõi.
                damageable.TakeDamage(_damagePerSecond);
                _nextDamageTimestamps.Add(damageable, Time.time + 1f);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Chỉ xét CapsuleCollider của người chơi.
            if (other is not CapsuleCollider) return;

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                // Khi đối tượng thoát ra, xóa nó khỏi danh sách theo dõi.
                _nextDamageTimestamps.Remove(damageable);
            }
        }
    }
}