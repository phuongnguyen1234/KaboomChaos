using UnityEngine;
using Bombs.Data;
using Core;
using Core.Interfaces;

namespace Bombs.Behaviors
{
    public class DynamiteBehavior : IBombBehavior
    {
        /// <summary>
        /// Khi được spawn, Dynamite không làm gì cả.
        /// </summary>
        public void OnActivate(BombController controller)
        {
            // Để trống
        }

        public void OnCollisionEnter(BombController controller, Collision collision)
        {
            TryFuseFromContact(controller, collision.gameObject);
        }

        public void OnCollisionStay(BombController controller, Collision collision)
        {
            // Liên tục kiểm tra khi đang tiếp xúc
            TryFuseFromContact(controller, collision.gameObject);
        }

        /// <summary>
        /// Kiểm tra xem đối tượng tiếp xúc có "nóng" không và kích hoạt ngòi nổ nếu cần.
        /// </summary>
        private void TryFuseFromContact(BombController controller, GameObject contactObject)
        {
            // Nếu bom đã được kích hoạt (đang cháy), không làm gì cả
            if (controller.IsActive) return;

            // Kiểm tra xem đối tượng va chạm có đang "cháy" không
            StatusEffectReceiver receiver = contactObject.GetComponentInParent<StatusEffectReceiver>();
            if (receiver != null && receiver.IsHot)
            {
                Debug.Log($"[DynamiteBehavior] Dynamite triggered by contact with a hot object '{contactObject.name}'. Starting fuse.");
                controller.StartFuse();
                return; // Đã kích hoạt, không cần kiểm tra thêm
            }

            // Kiểm tra xem đối tượng va chạm có phải là người chơi có Fire Shield không
            IPlayerShieldController shieldController = contactObject.GetComponentInParent<IPlayerShieldController>();
            if (shieldController != null && shieldController.HasFireShield)
            {
                Debug.Log($"[DynamiteBehavior] Dynamite triggered by contact with a Fire Shield player '{contactObject.name}'. Starting fuse.");
                controller.StartFuse();
            }
        }

        /// <summary>
        /// Áp dụng trọng lực để nó rơi xuống như một vật thể thông thường.
        /// </summary>
        public void OnFixedUpdate(BombController controller) { }

        public void OnSetup(BombController controller)
        {
            controller.BombCollider.isTrigger = false;
        }

        public void OnTriggerEnter(BombController controller, Collider other)
        {
            // Nếu bom đã được kích hoạt, không làm gì cả
            if (controller.IsActive) return;

            // Kiểm tra xem có đi vào vùng dung nham (hoặc vùng gây cháy nào khác) không
            DamageZoneController damageZone = other.GetComponent<DamageZoneController>();
            if (damageZone != null && damageZone.EffectToApply == StatusEffectType.Burning)
            {
                Debug.Log($"[DynamiteBehavior] Dynamite triggered by entering a burning zone '{other.name}'. Starting fuse.");
                controller.StartFuse();
            }
        }

        // Phương thức này sẽ được thêm vào interface ở bước sau
        public void OnExplosionHit(BombController controller, Vector3 force, Vector3 point, IBaseBombData triggeringBombData)
        {
            // CẢI TIẾN: Thêm logic để xử lý trường hợp DynamiteData bị cấu hình sai thành Landmine.
            // Nếu một quả bom được thiết lập để "kích hoạt khi va chạm" (isActivatedOnContact),
            // thì nó không nên được kích hoạt bởi một vụ nổ khác. Hành vi đó dành riêng cho Dynamite.
            if (controller.BombData is BombData normalBombData && normalBombData.IsActivatedOnContact)
            {
                // Coi nó như một quả bom thông thường và chỉ áp dụng lực.
                if (controller.BombRigidbody != null && !controller.BombRigidbody.isKinematic)
                    controller.BombRigidbody.AddForceAtPosition(force, point, triggeringBombData.ForceMode);
                return;
            }

            // Nếu bom chưa được kích hoạt
            if (!controller.IsActive) // và không phải là landmine (đã kiểm tra ở trên)
            {
                // Kiểm tra các hiệu ứng bị loại trừ
                var effect = triggeringBombData.Effect;
                if (effect == StatusEffectType.Frozen || effect == StatusEffectType.Electrified || effect == StatusEffectType.Poison)
                {
                    return; // Không kích hoạt
                }

                // Kích hoạt ngòi nổ
                Debug.Log($"[DynamiteBehavior] Dynamite triggered by explosion from '{triggeringBombData.DisplayName}'. Starting fuse.");
                controller.StartFuse();
            }
            else if (controller.BombRigidbody != null && !controller.BombRigidbody.isKinematic)
            {
                // Nếu bom đã được kích hoạt (ví dụ: đang cháy), nó vẫn sẽ bị đẩy đi bởi các vụ nổ khác.
                controller.BombRigidbody.AddForceAtPosition(force, point, triggeringBombData.ForceMode);
            }
        }
    }
}
