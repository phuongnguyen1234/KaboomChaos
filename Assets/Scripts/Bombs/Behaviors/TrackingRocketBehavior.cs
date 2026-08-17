using UnityEngine;
using System.Linq;
using Bombs.Data;
using Core.Interfaces;
using Managers; // Cần để truy cập PlayerManager

namespace Bombs.Behaviors
{
    /// <summary>
    /// Lớp "strategy" hành vi cho các loại tên lửa có khả năng theo dõi mục tiêu.
    /// </summary>
    public class TrackingRocketBehavior : IBombBehavior
    {
        private Transform _target;
        private Rigidbody _targetRigidbody; // Thêm biến để lưu Rigidbody của mục tiêu
        private IPlayerManager _playerManager;

        public void OnSetup(BombController controller)
        {
            // Tên lửa là collider vật lý để có thể va chạm và bị chặn bởi các vật thể.
            controller.BombCollider.isTrigger = false;
            if (controller.BombRigidbody != null)
            {
                controller.BombRigidbody.isKinematic = false;
                // Tên lửa tự di chuyển, không cần trọng lực.
                controller.BombRigidbody.useGravity = false;
            }

            // Lấy instance của PlayerManager để tìm mục tiêu.
            _playerManager = PlayerManager.Instance;
        }

        public void OnActivate(BombController controller)
        {
            if (controller.BombData is TrackingRocketData rocketData)
            {
                // Bắt đầu ngòi nổ ngay lập tức.
                // StartFuse() sẽ đặt IsActive = true và bắt đầu coroutine FuseBombRoutine,
                // coroutine này sẽ xử lý các fuse stage và gọi Explode() khi hết giờ.
                controller.StartFuse();
                // Tìm mục tiêu ban đầu cho việc di chuyển.
                FindTarget(controller, rocketData.trackingType);
            }
        }

        public void OnFixedUpdate(BombController controller)
        {
            if (!controller.IsActive || controller.BombData is not TrackingRocketData rocketData) return;

            // 1. Cập nhật mục tiêu
            // Nếu là Chasing, luôn tìm mục tiêu gần nhất.
            if (rocketData.trackingType == TrackingType.Chasing)
            {
                FindTarget(controller, rocketData.trackingType);
            }
            // Nếu là Homing, chỉ tìm mục tiêu mới nếu mục tiêu cũ không còn hợp lệ.
            else if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                FindTarget(controller, rocketData.trackingType);
            }

            // 2. Di chuyển và xoay
            if (_target != null)
            {
                // --- LOGIC CẢI TIẾN: XOAY MƯỢT MÀ THEO HƯỚNG DI CHUYỂN ---
                Vector3 directionToTarget = (_target.position - controller.transform.position).normalized;

                // Đặt vận tốc của tên lửa để nó bay theo hướng đó.
                controller.BombRigidbody.linearVelocity = directionToTarget * rocketData.speed;

                // Xoay tên lửa để mũi của nó (trục Y+) hướng theo hướng di chuyển (directionToTarget).
                // Điều này đảm bảo tên lửa luôn "nhìn" về nơi nó đang bay tới.
                if (directionToTarget != Vector3.zero)
                {
                    // Tính toán góc xoay mục tiêu.
                    // Chúng ta muốn trục Y+ (mũi tên lửa) hướng về phía mục tiêu.
                    // Để làm điều này với LookRotation, chúng ta cần cung cấp một hướng 'forward' và 'up'.
                    // Ta sẽ dùng 'directionToTarget' làm 'up'.
                    // Hướng 'forward' phải vuông góc với 'up'. Ta có thể dùng cross product để tìm nó.
                    Vector3 newForward = Vector3.Cross(directionToTarget, controller.transform.right);
                    // Nếu mục tiêu ở ngay trên hoặc dưới, cross product sẽ là zero.
                    if (newForward.sqrMagnitude < 0.001f)
                    {
                        newForward = controller.transform.forward; // Giữ hướng forward cũ
                    }
                    Quaternion targetRotation = Quaternion.LookRotation(newForward, directionToTarget);

                    // Sử dụng Quaternion.RotateTowards để xoay mượt mà với tốc độ turnSpeed (độ/giây).
                    Quaternion newRotation = Quaternion.RotateTowards(
                        controller.BombRigidbody.rotation,
                        targetRotation,
                        rocketData.turnSpeed * Time.fixedDeltaTime
                    );

                    // Sử dụng MoveRotation để xoay Rigidbody một cách an toàn trong FixedUpdate.
                    controller.BombRigidbody.MoveRotation(newRotation);
                }
            }
            else
            {
                // Nếu không có mục tiêu, bay thẳng về phía trước (theo hướng mũi Y+ hiện tại).
                controller.BombRigidbody.linearVelocity = controller.transform.up * rocketData.speed;
            }
        }
        public void OnTriggerEnter(BombController controller, Collider other)
        {
            if (!controller.IsActive) return;

            // Tên lửa chỉ nổ khi va chạm với người chơi.
            var playerComponent = other.GetComponentInParent<IPlayer>();
            if (playerComponent != null)
            {
                // Kiểm tra xem layer của người chơi có nằm trong danh sách bị ảnh hưởng không.
                LayerMask layersToTest = controller.BombData.TriggerLayers.value == 0 ? controller.BombData.AffectedLayers : controller.BombData.TriggerLayers;
                if ((layersToTest.value & (1 << other.gameObject.layer)) != 0)
                {
                    controller.Explode();
                }
            }
        }

        public void OnCollisionEnter(BombController controller, Collision collision)
        {
            if (!controller.IsActive) return;

            // Tên lửa chỉ nổ khi va chạm với người chơi.
            // Nếu va chạm với tường hoặc các vật thể khác, nó sẽ bị chặn lại.
            var playerComponent = collision.gameObject.GetComponentInParent<IPlayer>();
            if (playerComponent != null)
            {
                // Kiểm tra xem layer của người chơi có nằm trong danh sách bị ảnh hưởng không.
                LayerMask layersToTest = controller.BombData.TriggerLayers.value == 0 ? controller.BombData.AffectedLayers : controller.BombData.TriggerLayers;
                if ((layersToTest.value & (1 << collision.gameObject.layer)) != 0)
                {
                    controller.Explode();
                }
            }
        }

        // OnCollisionStay không cần thiết cho hành vi này.
        public void OnCollisionStay(BombController controller, Collision collision) { }

        public void OnExplosionHit(BombController controller, Vector3 force, Vector3 point, IBaseBombData triggeringBombData)
        {
            // Theo yêu cầu, tên lửa hoàn toàn miễn nhiễm với lực từ các vụ nổ khác.
            // Do đó, phương thức này sẽ không làm gì cả.
        }

        private void FindTarget(BombController controller, TrackingType type)
        {
            if (_playerManager == null) return;
            
            // Lấy danh sách những người chơi đang thực sự tham gia round đấu và còn sống.
            // Tên lửa sẽ nhắm cả những người chơi đang bị đóng băng, vì họ là mục tiêu dễ dàng.
            var validTargets = _playerManager.GetPlayersInRound()
                                             .Where(p => p.GameObject != null && p.GameObject.activeInHierarchy)
                                             .ToList();

            if (validTargets.Count == 0) 
            { 
                _target = null; 
                _targetRigidbody = null; // Reset Rigidbody khi không có mục tiêu
                return; 
            }

            if (type == TrackingType.Homing)
            {
                // Nếu đã có mục tiêu và mục tiêu đó vẫn hợp lệ, không tìm mục tiêu mới.
                if (_target != null && validTargets.Any(p => p.GameObject.transform == _target)) return;

                // Tìm một mục tiêu ngẫu nhiên mới và lưu lại cả transform và rigidbody.
                IPlayer chosenPlayer = validTargets[Random.Range(0, validTargets.Count)];
                _target = chosenPlayer.GameObject.transform;
                _targetRigidbody = chosenPlayer.GameObject.GetComponent<Rigidbody>();
            }
            else // Chasing
            {
                // Luôn tìm mục tiêu gần nhất và cập nhật transform/rigidbody.
                IPlayer chosenPlayer = validTargets.OrderBy(p => (p.GameObject.transform.position - controller.transform.position).sqrMagnitude).First();
                _target = chosenPlayer.GameObject.transform;
                _targetRigidbody = chosenPlayer.GameObject.GetComponent<Rigidbody>();
            }
        }
    }
}