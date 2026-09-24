using UnityEngine;
using Core.Interfaces; // Cần để sử dụng interface IDamageable

namespace Core
{
    /// <summary>
    /// Quản lý một vùng nguy hiểm (như dung nham, khí độc), gây sát thương theo thời gian cho bất kỳ đối tượng
    /// nào có interface IDamageable đi vào vùng ảnh hưởng.
    /// </summary>
    public class DamageZoneController : BaseAreaDamager
    {
        [Header("Status Effect Settings")]
        [Tooltip("Hiệu ứng sẽ được áp dụng cho các đối tượng trong vùng (ví dụ: Burning cho dung nham).")]
        [SerializeField] private StatusEffectType _effectToApply = StatusEffectType.Burning;
        [Tooltip("Thời gian hiệu ứng tồn tại (giây) sau khi một đối tượng thoát khỏi vùng.")]
        [SerializeField] private float _effectDurationOnExit = 5f;

        /// <summary>Hiệu ứng mà vùng này áp dụng.</summary>
        public StatusEffectType EffectToApply => _effectToApply;

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
                    ApplyDamage(damageable, DamageSourceType.EnvironmentalContact, _effectToApply);
                }
            }

            // --- 2. Áp dụng hiệu ứng trạng thái cho các đối tượng có thể bị ảnh hưởng (như DestructiblePart) ---
            // Logic này không cần marker và sẽ áp dụng cho bất kỳ đối tượng nào có StatusEffectReceiver.
            // SỬA LỖI: Nếu đối tượng là người chơi (đã nhận sát thương trực tiếp từ vùng),
            // không áp dụng hiệu ứng trạng thái Burning lặp lại để tránh gây sát thương hai lần.
            if (other.GetComponent<IPrimaryExplosionTarget>() != null) {
                return;
            }

            IStatusEffectable statusEffectable = other.GetComponentInParent<IStatusEffectable>();
            if (statusEffectable != null && _effectToApply != StatusEffectType.None)
            {
                // NEW LOGIC: Phân biệt giữa DestructibleBlock và DestructiblePart cho hiệu ứng Burning.
                if (_effectToApply == StatusEffectType.Burning)
                {
                    // CẢI TIẾN: Lấy component trực tiếp từ 'other' để đảm bảo tìm thấy đúng đối tượng.
                    DestructiblePart part = other.GetComponentInParent<DestructiblePart>();
                    DestructibleBlock block = other.GetComponentInParent<DestructibleBlock>();

                    if (part != null)
                    {
                        // Áp dụng Burning cho DestructiblePart như dự định ban đầu.
                        statusEffectable.ApplyStatusEffect(_effectToApply, _damageInterval * 2);
                    }
                    else if (block != null)
                    {
                        // Đối với DestructibleBlock, chỉ đặt cờ _isInLavaZone.
                        // Chúng ta KHÔNG áp dụng Burning làm _currentEffect của nó.
                        StatusEffectReceiver receiver = block.GetComponent<StatusEffectReceiver>();
                        if (receiver != null)
                        {
                            receiver.SetIsInLavaZone(true);
                        }
                    }
                }
                else // Đối với các hiệu ứng khác (ví dụ: Electrified, Frozen từ các nguồn khác), áp dụng chung.
                {
                    statusEffectable.ApplyStatusEffect(_effectToApply, _damageInterval * 2);
                }
            }
        }

        protected override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other); // Gọi lớp cơ sở để xóa đối tượng khỏi danh sách theo dõi sát thương.

            // --- 2. Áp dụng hiệu ứng tồn tại sau khi thoát ---
            IStatusEffectable statusEffectable = other.GetComponentInParent<IStatusEffectable>();
            if (statusEffectable != null && _effectToApply != StatusEffectType.None)
            {
                // NEW LOGIC: Xử lý _isInLavaZone cho DestructibleBlock khi thoát khỏi dung nham.
                if (_effectToApply == StatusEffectType.Burning)
                {
                    DestructibleBlock block = (statusEffectable as MonoBehaviour)?.GetComponent<DestructibleBlock>();
                    if (block != null)
                    {
                        StatusEffectReceiver receiver = (statusEffectable as MonoBehaviour)?.GetComponent<StatusEffectReceiver>();
                        if (receiver != null)
                        {
                            receiver.SetIsInLavaZone(false);
                        }
                    }
                    else
                    {
                        // Đối với các đối tượng khác (như DestructiblePart), áp dụng thời gian tồn tại hiệu ứng khi thoát.
                        // Người chơi sẽ không bị đốt cháy sau khi thoát khỏi dung nham.
                        // SỬA LỖI: Kiểm tra trực tiếp sự tồn tại của IPlayer để loại trừ người chơi một cách đáng tin cậy.
                        // Điều này tránh các vấn đề khi IPrimaryExplosionTarget không có trên collider đã kích hoạt sự kiện.
                        if (other.GetComponentInParent<IPlayer>() == null)
                        {
                            statusEffectable.ApplyStatusEffect(_effectToApply, _effectDurationOnExit);
                        }
                    }
                }
                else // Đối với các hiệu ứng khác, áp dụng chung.
                {
                    statusEffectable.ApplyStatusEffect(_effectToApply, _effectDurationOnExit);
                }
            }
        }
    }
}