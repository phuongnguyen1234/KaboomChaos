using Bombs.Data;
using UnityEngine;

namespace Bombs.Behaviors
{
    /// <summary>
    /// Lớp "strategy" hành vi cụ thể cho các loại bom hẹn giờ tiêu chuẩn.
    /// </summary>
    public class FuseBombBehavior : IBombBehavior // File này nên được đặt trong Assets/Scripts/Bombs/Behaviors/
    {
        public void OnSetup(BombController controller)
        {
            controller.BombCollider.isTrigger = false; // Bom hẹn giờ là collider vật lý rắn
        }

        public void OnActivate(BombController controller)
        {
            if (controller.BombData is BombData fuseBombData)
            {
                // Nếu bom không được kích hoạt khi va chạm, hãy bắt đầu ngòi nổ ngay lập tức.
                // Đây là hành vi của một quả bom hẹn giờ tiêu chuẩn.
                if (!fuseBombData.isActivatedOnContact)
                {
                    controller.StartFuse();
                }
                // Nếu bom được kích hoạt khi va chạm (isActivatedOnContact = true),
                // chúng ta không làm gì ở đây. Bom sẽ chỉ rơi xuống và chờ va chạm.
                // Logic trong OnCollisionEnter sẽ xử lý việc bắt đầu ngòi nổ.
            }
        }

        public void OnFixedUpdate(BombController controller)
        {
        }

        public void OnCollisionEnter(BombController controller, Collision collision)
        {
            // Xử lý cho bom mìn (landmine) khi va chạm với một collider vật lý.
            TryFuseOnContact(controller, collision.gameObject);
        }

        public void OnCollisionStay(BombController controller, Collision collision)
        {
            // Bom hẹn giờ tiêu chuẩn không có hành vi đặc biệt khi tiếp xúc liên tục.
        }

        public void OnTriggerEnter(BombController controller, Collider other)
        {
            // Xử lý cho bom mìn (landmine) khi một trigger (như hurtbox của player) đi vào nó.
            TryFuseOnContact(controller, other.gameObject);
        }

        public void OnExplosionHit(BombController controller, Vector3 force, Vector3 point, Core.Interfaces.IBaseBombData triggeringBombData)
        {
            // Bom hẹn giờ tiêu chuẩn chỉ bị đẩy đi bởi các vụ nổ khác.
            // Chúng ta áp dụng lực vật lý ở đây.
            if (controller.BombRigidbody != null && !controller.BombRigidbody.isKinematic)
                controller.BombRigidbody.AddForceAtPosition(force, point, triggeringBombData.ForceMode);
        }

        /// <summary>
        /// Kiểm tra và kích hoạt ngòi nổ cho bom mìn (landmine) khi có va chạm.
        /// Được gọi từ cả OnCollisionEnter và OnTriggerEnter để xử lý cả va chạm vật lý và trigger.
        /// </summary>
        private void TryFuseOnContact(BombController controller, GameObject contactObject)
        {
            // Chỉ xử lý cho bom mìn (isActivatedOnContact) và khi nó chưa được kích hoạt.
            if (controller.BombData is BombData fuseBombData && fuseBombData.isActivatedOnContact && !controller.IsActive)
            {
                var bombData = controller.BombData;
                // Ưu tiên sử dụng TriggerLayers, nếu không thì dùng AffectedLayers.
                LayerMask layersToTest = bombData.TriggerLayers.value == 0 ? bombData.AffectedLayers : bombData.TriggerLayers;

                // Kiểm tra xem layer của đối tượng va chạm có nằm trong danh sách được phép kích hoạt không.
                if ((layersToTest.value & (1 << contactObject.layer)) != 0)
                {
                    controller.StartFuse();
                }
            }
        }
    }
}