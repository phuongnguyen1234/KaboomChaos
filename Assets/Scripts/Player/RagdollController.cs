using UnityEngine;
using System.Collections;

namespace Player
{
    /// <summary>
    /// Quản lý trạng thái vật lý ragdoll của nhân vật.
    /// Chịu trách nhiệm bật/tắt các component vật lý và điều khiển để chuyển đổi giữa trạng thái hoạt ảnh và ragdoll.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerAnimator))]
    [RequireComponent(typeof(CameraController))]
    [RequireComponent(typeof(PlayerInputAdapter))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class RagdollController : MonoBehaviour
    {
        #region Fields

        [Header("Ragdoll Settings")]
        [Tooltip("Kéo GameObject gốc chứa tất cả các bộ phận ragdoll vào đây.")]
        [SerializeField] private Transform _ragdollRoot;

        [Tooltip("Mục tiêu camera sẽ theo dõi khi ragdoll (ví dụ: đầu hoặc hông). Nếu bỏ trống, camera sẽ không đổi mục tiêu.")]
        [SerializeField] private Transform _ragdollFollowTarget;

        [Header("Recovery Settings")]
        [Tooltip("Thời gian (giây) người chơi bị khóa trước khi có thể đứng dậy.")]
        [SerializeField] private float _recoveryDelay = 3.0f;

        [Header("Death Settings")]
        [Tooltip("Vật liệu vật lý để áp dụng cho các bộ phận khi chết, tạo độ nảy. Nếu bỏ trống, sẽ không có hiệu ứng nảy.")]
        [SerializeField] private PhysicsMaterial _deathBouncyMaterial;

        [Header("Ragdoll Movement")]
        [Tooltip("Lực để 'lê lết' khi đang ở trạng thái ragdoll. Đặt là 0 để vô hiệu hóa. Hoạt động tốt nhất khi các Rigidbody của ragdoll có giá trị Drag > 0 (ví dụ: 1 hoặc 2).")]
        [SerializeField] private float _ragdollMoveSpeed = 50f;

        // Components to disable when ragdoll is active
        private PlayerController _playerController;
        private Animator _animator;
        private PlayerAnimator _playerAnimator;
        private CameraController _cameraController;
        private PlayerInputAdapter _inputAdapter;
        private Rigidbody _mainRigidbody;
        private CapsuleCollider _mainCollider;

        // Ragdoll components
        private Rigidbody[] _ragdollRigidbodies;
        private Collider[] _ragdollColliders;

        // State properties
        public bool IsRagdollActive { get; private set; } = false;
        private bool _isRecoverable = false;
        private Coroutine _recoveryCoroutine;

        // Transform của camera để tính toán hướng di chuyển
        private Transform _cameraTransform;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Cache all necessary components
            _playerController = GetComponent<PlayerController>();
            _animator = GetComponent<Animator>();
            _playerAnimator = GetComponent<PlayerAnimator>();
            _cameraController = GetComponent<CameraController>();
            _inputAdapter = GetComponent<PlayerInputAdapter>();
            _mainRigidbody = GetComponent<Rigidbody>();
            _mainCollider = GetComponent<CapsuleCollider>();

            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            if (_ragdollRoot != null)
            {
                _ragdollRigidbodies = _ragdollRoot.GetComponentsInChildren<Rigidbody>();
                _ragdollColliders = _ragdollRoot.GetComponentsInChildren<Collider>();
                
                // Initial setup
                SetRagdollState(false);
                IgnoreSelfCollision();
            }
            else
            {
                Debug.LogError("Ragdoll root has not been assigned! Ragdoll functionality will be disabled.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Nếu đang trong trạng thái ragdoll và có thể hồi phục, chờ input để đứng dậy.
            if (IsRagdollActive && _isRecoverable)
            {
                // Người chơi có thể chủ động đứng dậy bằng cách ấn phím nhảy.
                if (_inputAdapter.IsJumpKeyPressed())
                {
                    SetRagdollState(false);
                }
            }
        }

        private void FixedUpdate()
        {
            // Chỉ xử lý di chuyển ragdoll khi nó đang hoạt động.
            if (IsRagdollActive)
            {
                HandleRagdollMovement();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Kích hoạt hoặc vô hiệu hóa trạng thái ragdoll.
        /// Bất kỳ hệ thống nào (máu, bom, input) cũng có thể gọi phương thức này.
        /// </summary>
        /// <param name="state">True để bật ragdoll, False để tắt.</param>
        public void SetRagdollState(bool state)
        {
            if (IsRagdollActive == state || !enabled) return;
            IsRagdollActive = state;

            if (state)
            {
                // --- KÍCH HOẠT RAGDOLL ---

                // Lấy vận tốc cuối cùng của PlayerController TRƯỚC KHI nó bị vô hiệu hóa.
                Vector3 lastVelocity = _playerController.GetVelocity();

                // 1. Vô hiệu hóa các component điều khiển
                if (_animator != null) _animator.enabled = false;
                _playerController.enabled = false;
                _mainCollider.enabled = false;
                if (_mainRigidbody != null) _mainRigidbody.isKinematic = true;

                // 2. Kích hoạt các component vật lý của ragdoll
                foreach (var rb in _ragdollRigidbodies)
                {
                    rb.isKinematic = false;
                    // Áp dụng vận tốc cuối cùng của nhân vật cho mỗi bộ phận của ragdoll
                    // để bảo toàn động lượng.
                    rb.linearVelocity = lastVelocity;
                }
                foreach (var col in _ragdollColliders) { col.enabled = true; }

                // Khi bật ragdoll, bắt đầu hoặc reset lại bộ đếm thời gian hồi phục.
                ResetRecoveryTimer();

                // Yêu cầu CameraController chuyển target theo dõi sang mục tiêu của ragdoll
                if (_cameraController != null && _ragdollFollowTarget != null)
                {
                    _cameraController.SetFollowTarget(_ragdollFollowTarget);
                }
            }
            else
            {
                // --- TẮT RAGDOLL (ĐỨNG DẬY) ---

                // 1. Dừng coroutine hồi phục và vô hiệu hóa các component của ragdoll
                if (_recoveryCoroutine != null)
                {
                    StopCoroutine(_recoveryCoroutine);
                    _recoveryCoroutine = null;
                }
                _isRecoverable = false;
                foreach (var rb in _ragdollRigidbodies) { rb.isKinematic = true; }
                foreach (var col in _ragdollColliders) { col.enabled = false; }

                // 2. Dịch chuyển GameObject điều khiển ("hồn") đến vị trí của ragdoll ("xác").
                // Đây là bước quan trọng nhất để đảm bảo controller "thức dậy" ở đúng vị trí.
                if (_ragdollRigidbodies.Length > 0)
                {
                    Vector3 hipPosition = _ragdollRigidbodies[0].position;
                    transform.position = hipPosition;
                }

                // 3. Kích hoạt lại các component vật lý và hình ảnh.
                _mainCollider.enabled = true;
                if (_mainRigidbody != null) _mainRigidbody.isKinematic = false;
                if (_animator != null) _animator.enabled = true;

                // 4. Reset trạng thái của các hệ thống phụ thuộc (camera, animator) TRƯỚC KHI
                // bật lại bộ điều khiển chính. Điều này cực kỳ quan trọng để ngăn Animator
                // (với root motion) hoặc các logic khác ghi đè lên vị trí mới của nhân vật.
                if (_cameraController != null) _cameraController.ResetFollowTarget();
                if (_playerAnimator != null) _playerAnimator.TriggerReset();

                // 5. Kích hoạt lại PlayerController sau cùng.
                // Lúc này, PlayerController.OnEnable() sẽ chạy trên một GameObject đã ở đúng vị trí
                // và trạng thái, tránh được lỗi teleport.
                _playerController.enabled = true;
            }
        }

        /// <summary>
        /// Kích hoạt trạng thái ragdoll và phá hủy tất cả các khớp để tạo hiệu ứng "vỡ ra" như Roblox.
        /// </summary>
        public void ShatterAndDie()
        {
            // 1. Kích hoạt trạng thái ragdoll cơ bản.
            if (!IsRagdollActive)
            {
                SetRagdollState(true);
            }

            // 2. Áp dụng vật liệu vật lý nảy cho tất cả các collider của ragdoll.
            if (_deathBouncyMaterial != null && _ragdollColliders != null)
            {
                foreach (var col in _ragdollColliders)
                {
                    col.material = _deathBouncyMaterial;
                }
            }

            // 3. Tìm và phá hủy tất cả các khớp (Joints) trong hệ thống ragdoll.
            if (_ragdollRoot != null)
            {
                foreach (var joint in _ragdollRoot.GetComponentsInChildren<Joint>())
                {
                    Destroy(joint);
                }
            }
        }

        /// <summary>
        /// Kích hoạt ragdoll và áp dụng một lực ban đầu.
        /// Hữu ích cho các hiệu ứng vụ nổ.
        /// </summary>
        /// <param name="force">Vector lực để áp dụng.</param>
        /// <param name="point">Điểm tác dụng lực.</param>
        public void EnableRagdollWithForce(Vector3 force, Vector3 point)
        {
            // Đảm bảo nhân vật đang ở trạng thái ragdoll.
            if (!IsRagdollActive)
            {
                SetRagdollState(true);
            }

            // Tìm Rigidbody gần nhất với điểm nổ để áp dụng lực
            Rigidbody closestRb = GetClosestRigidbody(point);
            if (closestRb != null)
            {
                closestRb.AddForceAtPosition(force, point, ForceMode.Impulse);
            }

            // Mỗi khi nhận một lực mạnh, reset lại thời gian có thể đứng dậy.
            ResetRecoveryTimer();
        }

        #endregion

        #region Coroutines

        /// <summary>
        /// Coroutine đếm ngược thời gian trước khi người chơi có thể đứng dậy.
        /// </summary>
        private IEnumerator RecoveryTimer()
        {
            yield return new WaitForSeconds(_recoveryDelay);
            _isRecoverable = true;
            // Tại đây có thể thêm hiệu ứng âm thanh/hình ảnh để báo cho người chơi biết họ có thể đứng dậy.
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Xử lý di chuyển của nhân vật khi đang ở trạng thái ragdoll (lê lết).
        /// </summary>
        private void HandleRagdollMovement()
        {
            // Yêu cầu: có tốc độ, có các component cần thiết, và ragdoll đang nằm trên mặt đất.
            if (_ragdollMoveSpeed <= 0f || _ragdollRigidbodies.Length == 0 || _playerController == null || !IsRagdollGrounded())
            {
                return;
            }

            // 1. Lấy hướng di chuyển từ PlayerController để đảm bảo logic thống nhất.
            // PlayerController bị vô hiệu hóa khi ragdoll, nhưng phương thức này vẫn hoạt động
            // vì nó chỉ đọc input và transform, không phụ thuộc vào vòng lặp Update/FixedUpdate của controller.
            Vector3 moveDirection = _playerController.GetMovementDirection();
            if (moveDirection.sqrMagnitude < 0.01f)
            {
                return; // Không có input, không làm gì cả.
            }

            // 2. Áp dụng lực vào phần hông của ragdoll (thường là Rigidbody đầu tiên trong mảng)
            Rigidbody rootRb = _ragdollRigidbodies[0];
            if (rootRb != null)
            {
                Vector3 moveForce = moveDirection * _ragdollMoveSpeed;

                // Sử dụng ForceMode.Force để tạo cảm giác kéo/lê một cách vật lý.
                // Lực này sẽ chống lại 'Drag' của Rigidbody, tạo ra một tốc độ tối đa có thể kiểm soát.
                rootRb.AddForce(moveForce, ForceMode.Force);
            }
        }

        /// <summary>
        /// Kiểm tra xem ragdoll có đang chạm đất hay không.
        /// </summary>
        /// <returns>True nếu phần hông của ragdoll ở gần mặt đất.</returns>
        private bool IsRagdollGrounded()
        {
            if (_ragdollRigidbodies.Length == 0) return false;

            Rigidbody rootRb = _ragdollRigidbodies[0];
            if (rootRb == null) return false;

            // Bắn một SphereCast ngắn xuống dưới từ vị trí hông để kiểm tra mặt đất.
            // Bắt đầu cast từ một điểm hơi cao hơn một chút để tránh trường hợp hông đã lún vào trong đất.
            float sphereRadius = 0.15f;
            float castDistance = 0.2f;
            Vector3 castOrigin = rootRb.position + Vector3.up * 0.1f;

            // Bỏ qua va chạm với chính các bộ phận của ragdoll (mặc dù chúng không nên ở dưới hông)
            // LayerMask sẽ bỏ qua tất cả các trigger.
            return Physics.SphereCast(castOrigin, sphereRadius, Vector3.down, out _, castDistance, ~0, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Reset lại bộ đếm thời gian cho đến khi người chơi có thể đứng dậy.
        /// </summary>
        private void ResetRecoveryTimer()
        {
            if (_recoveryCoroutine != null)
            {
                StopCoroutine(_recoveryCoroutine);
            }
            _isRecoverable = false;
            _recoveryCoroutine = StartCoroutine(RecoveryTimer());
        }

        /// <summary>
        /// Vô hiệu hóa va chạm giữa các bộ phận của cùng một ragdoll.
        /// </summary>
        private void IgnoreSelfCollision()
        {
            if (_ragdollColliders == null || _ragdollColliders.Length <= 1) return;

            for (int i = 0; i < _ragdollColliders.Length; i++)
            {
                for (int j = i + 1; j < _ragdollColliders.Length; j++)
                {
                    Physics.IgnoreCollision(_ragdollColliders[i], _ragdollColliders[j]);
                }
            }
        }

        /// <summary>
        /// Tìm Rigidbody của ragdoll gần nhất với một điểm cho trước.
        /// </summary>
        private Rigidbody GetClosestRigidbody(Vector3 point)
        {
            Rigidbody closestRb = null;
            float minDistance = float.MaxValue;

            foreach (var rb in _ragdollRigidbodies)
            {
                float distance = Vector3.Distance(rb.position, point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestRb = rb;
                }
            }
            return closestRb;
        }

        #endregion
    }
}