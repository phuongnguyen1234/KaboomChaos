using Core.Interfaces;
using UnityEngine;

namespace Core.Miscellaneous
{
    /// <summary>
    /// Điều khiển hành vi của một băng chuyền, di chuyển các đối tượng Rigidbody đứng trên nó.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))] // QUAN TRỌNG: Băng chuyền phải có Collider để hoạt động.
    public class ConveyorBelt : MonoBehaviour
    {
        [Tooltip("Tốc độ và hướng di chuyển của băng chuyền trong không gian cục bộ của nó (m/s).")]
        [SerializeField] private Vector3 _localVelocity = new(0, 0, 2f);

        /// <summary>
        /// Vận tốc của băng chuyền trong không gian thế giới.
        /// </summary>
        public Vector3 WorldVelocity { get; private set; }

        private Rigidbody _rb;

        private void Awake()
        {
            // Băng chuyền cần một Rigidbody để tương tác vật lý đúng cách.
            // Nó nên là kinematic để không bị di chuyển bởi các lực bên ngoài.
            if (!TryGetComponent(out _rb))
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }
            _rb.isKinematic = true;
            _rb.useGravity = false;

            // Tính toán vận tốc trong không gian thế giới một lần.
            WorldVelocity = transform.TransformDirection(_localVelocity);
        }

        private void FixedUpdate()
        {
            // Băng chuyền không còn tự di chuyển.
            // Việc di chuyển người chơi sẽ được PlayerController xử lý trực tiếp thông qua HandleMomentum.
            // Việc di chuyển các vật thể khác vẫn được xử lý trong OnCollisionStay.
        }

        // Phương thức này xử lý các đối tượng Rigidbody thông thường (không phải PlayerController).
        // PlayerController được xử lý riêng để tích hợp với logic di chuyển phức tạp của nó.
        private void OnCollisionStay(Collision collision)
        {
            // Chỉ tác động lên các Rigidbody động.
            if (collision.rigidbody != null && !collision.rigidbody.isKinematic)
            {
                // Để tránh xung đột với PlayerController, chúng ta sẽ không tác động lên người chơi ở đây.
                // PlayerController (triển khai IPlayer) được xử lý tự động bởi hệ thống 'groundMomentum' của CMF.
                // Sử dụng 'GetComponentInParent' để đảm bảo phát hiện được IPlayer ngay cả khi va chạm với một bộ phận con.
                if (collision.gameObject.GetComponentInParent<IPlayer>() != null)
                {
                    return;
                }

                Rigidbody otherRb = collision.rigidbody;
                
                // Tính toán sự thay đổi vận tốc cần thiết để làm cho đối tượng di chuyển cùng với băng chuyền.
                Vector3 velocityChange = WorldVelocity - otherRb.linearVelocity;

                // Chỉ áp dụng lực theo hướng của băng chuyền để không ảnh hưởng đến các lực khác (như trọng lực).
                Vector3 force = Vector3.Project(velocityChange, WorldVelocity.normalized);
                
                // Sử dụng ForceMode.VelocityChange để áp dụng thay đổi vận tốc tức thì, mô phỏng bề mặt đang di chuyển.
                otherRb.AddForce(force, ForceMode.VelocityChange);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 worldVelocity = transform.TransformDirection(_localVelocity);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + worldVelocity);
        }
    }
}