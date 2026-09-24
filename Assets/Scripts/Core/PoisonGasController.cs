using UnityEngine;
using System.Collections.Generic;
using Core.Utilities;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Quản lý hành vi của một vùng khí độc.
    /// Gây sát thương theo thời gian cho các đối tượng bên trong trigger
    /// và tự cấu hình hình dạng của particle system.
    /// </summary>
    [RequireComponent(typeof(BoxCollider), typeof(LifetimeController))]
    public class PoisonGasController : BaseAreaDamager
    {
        // --- Cached Components ---
        private ParticleSystem _particleSystem;
        private BoxCollider _triggerCollider;

        protected override void Awake()
        {
            base.Awake(); // Gọi Awake của lớp cơ sở để kiểm tra trigger
            // Tìm ParticleSystem trong các object con (kể cả khi nó đang bị tắt).
            // Điều này cho phép tách riêng prefab của hiệu ứng hình ảnh.
            _particleSystem = GetComponentInChildren<ParticleSystem>(true);
            _triggerCollider = GetComponent<BoxCollider>();

            if (_particleSystem == null)
            {
                Debug.LogWarning("[PoisonGasController] Không tìm thấy ParticleSystem nào trong các object con. Hiệu ứng khí độc sẽ không có hình ảnh.", this);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable(); // Gọi OnEnable của lớp cơ sở để reset dictionary

            // Tự động cấu hình hình dạng của particle system để khớp với trigger collider.
            ConfigureParticleSystemShape();
        }

        private void OnTriggerStay(Collider other)
        {
            // Kiểm tra xem đối tượng va chạm có thuộc layer có thể nhận sát thương không.
            if ((_damageableLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            // Tìm component có thể nhận sát thương.
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) return;

            // Ủy quyền việc gây sát thương (có kiểm tra thời gian chờ) cho lớp cơ sở.
            // _damageAmount và _damageInterval được lấy từ lớp cơ sở.
            ApplyDamage(damageable, DamageSourceType.StatusEffectContact, StatusEffectType.Poison);
        }

        /// <summary>
        /// Cấu hình module Shape của Particle System để phát hạt từ bề mặt của trigger.
        /// Logic này được tham khảo từ StatusEffectReceiver.
        /// Yêu cầu: Prefab của Particle System phải được xoay sẵn nếu cần (ví dụ: xoay -90 độ ở trục X để khí bốc lên).
        /// </summary>
        private void ConfigureParticleSystemShape()
        {
            if (_particleSystem == null) return;

            // Ủy quyền việc cấu hình cho lớp tiện ích, yêu cầu phát hạt từ bên trong thể tích (Volume).
            // Lớp tiện ích sẽ giữ nguyên góc xoay của prefab, vì vậy hãy đảm bảo prefab được thiết lập đúng hướng.
            ParticleSystemUtils.MatchShapeToBoxVolume(_particleSystem, gameObject);
        }
    }
}