using UnityEngine;
using Core;
using Bombs.Data;

namespace Bombs.Behaviors
{
    /// <summary>
    /// Lớp "strategy" hành vi cụ thể cho các loại bom dạng tên lửa.
    /// </summary>
    public class MissileBombBehavior : IBombBehavior // File này nên được đặt trong Assets/Scripts/Bombs/Behaviors/
    {
        public void OnSetup(BombController controller)
        {
            controller.BombCollider.isTrigger = true; // Tên lửa nên đi xuyên qua vật thể
            // Ensure the rigidbody is not kinematic so its velocity can be set by the behavior.
            if (controller.BombRigidbody != null)
            {
                controller.BombRigidbody.isKinematic = false;
            }
        }

        public void OnActivate(BombController controller)
        {
            controller.SetActivationState(true);
            // Chuyển động của tên lửa được xử lý trong FixedUpdate. Không cần coroutine khi kích hoạt.
            controller.SetActiveCoroutine(null);

            // --- LOGIC MỚI: TẠO CHỈ BÁO ĐIỂM RƠI ---
            // Khi tên lửa được kích hoạt, bắn một tia từ vị trí hiện tại xuống dưới để tìm điểm va chạm dự kiến.
            // Sau đó, tạo một hiệu ứng (decal, ánh sáng) tại điểm đó để cảnh báo người chơi.
            if (controller.BombData is MissileBombData missileBombData && missileBombData.landingLightVFX != null)
            {
                // Xác định các layer mà tia sẽ va chạm, tương tự như logic nổ.
                LayerMask layersToTest = controller.BombData.TriggerLayers.value == 0 ? controller.BombData.AffectedLayers : controller.BombData.TriggerLayers;

                // Bắn tia xuống dưới để tìm điểm va chạm.
                if (Physics.Raycast(controller.transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, layersToTest, QueryTriggerInteraction.Ignore))
                {
                    // Tạo hiệu ứng chỉ báo tại điểm va chạm.
                    // Xoay hiệu ứng để nó nằm trên bề mặt (dựa vào pháp tuyến của điểm va chạm).
                    Quaternion indicatorRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    GameObject indicatorInstance = GameEvents.TriggerVFXSpawnRequest(missileBombData.landingLightVFX, hit.point, indicatorRotation);

                    if (indicatorInstance != null)
                    {
                        // --- LOGIC MỚI: TÙY CHỈNH MÀU ÁNH SÁNG ---
                        // Nếu VFX có component Light, hãy đặt màu của nó theo cấu hình trong Data.
                        if (indicatorInstance.TryGetComponent<Light>(out var lightComponent))
                        {
                            lightComponent.color = missileBombData.landingLightColor;
                        }

                        // Giao instance của hiệu ứng cho BombController quản lý và dọn dẹp.
                        controller.SetLandingIndicator(indicatorInstance);
                    }
                }
            }
        }

        public void OnFixedUpdate(BombController controller)
        {
            // Chỉ điều khiển chuyển động nếu bom đang hoạt động.
            if (controller.IsActive && controller.BombData is MissileBombData missileData)
            {
                // Tên lửa rơi với tốc độ không đổi
                controller.BombRigidbody.linearVelocity = Vector3.down * missileData.fallSpeed;
            }
        }

        public void OnCollisionEnter(BombController controller, Collision collision)
        {
            // Tên lửa là trigger, nên không dùng hàm này.
        }

        public void OnCollisionStay(BombController controller, Collision collision)
        {
            // Tên lửa là trigger, nên không dùng hàm này.
        }

        public void OnTriggerEnter(BombController controller, Collider other)
        {
            // Logic cho bom dạng trigger (tên lửa) phát nổ khi va chạm.
            var bombData = controller.BombData;

            // Ưu tiên sử dụng triggerLayers nếu nó được định nghĩa (khác Nothing).
            // Nếu không, quay lại sử dụng affectedLayers mặc định.
            LayerMask layersToTest = bombData.TriggerLayers.value == 0 ? bombData.AffectedLayers : bombData.TriggerLayers;

            // Kiểm tra layer để tránh nổ trên các trigger khác hoặc các đối tượng không thuộc gameplay.
            if ((layersToTest.value & (1 << other.gameObject.layer)) != 0)
            {
                controller.Explode();
            }
        }

        public void OnExplosionHit(BombController controller, Vector3 force, Vector3 point, Core.Interfaces.IBaseBombData triggeringBombData)
        {
            // Bom tên lửa cũng chỉ bị đẩy đi bởi các vụ nổ khác.
            // Chúng ta áp dụng lực vật lý ở đây.
            if (controller.BombRigidbody != null && !controller.BombRigidbody.isKinematic)
                controller.BombRigidbody.AddForceAtPosition(force, point, triggeringBombData.ForceMode);
        }
    }
}