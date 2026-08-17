using UnityEngine;
using System.Collections;
using Core.Interfaces;

using Core; // Cần cho GameEvents và StatusEffectType

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
    public class RagdollController : MonoBehaviour, IExplosionReactable
    {
        #region Fields

        [Header("Ragdoll Settings")]
        [Tooltip("Kéo GameObject gốc chứa tất cả các bộ phận ragdoll vào đây.")]
        [SerializeField] private Transform _ragdollRoot;

        [Tooltip("Hurtbox riêng dùng để nhận sát thương khi ở trạng thái bình thường (thường là một trigger collider con).")]
        [SerializeField] private Collider _damageHurtbox;

        [Tooltip("Mục tiêu camera sẽ theo dõi khi ragdoll (ví dụ: đầu hoặc hông). Nếu bỏ trống, camera sẽ không đổi mục tiêu.")]
        [SerializeField] private Transform _ragdollFollowTarget;

        [Header("Recovery Settings")]
        [Tooltip("Thời gian (giây) người chơi bị khóa trước khi có thể đứng dậy.")]
        [SerializeField] private float _recoveryDelay = 3.0f;
        [Tooltip("Lực tối thiểu của vụ nổ để kích hoạt ragdoll. Nếu lực tác động nhỏ hơn giá trị này, người chơi sẽ chỉ bị đẩy đi thay vì ngã.")]
        [SerializeField] private float _ragdollForceThreshold = 500f;
        [Tooltip("Bật/tắt tính năng ragdoll khi bị trúng đòn (nhưng chưa chết). Nếu tắt, người chơi sẽ chỉ bị đẩy đi. Hiệu ứng 'vỡ ra' khi chết vẫn hoạt động.")]
        [SerializeField] private bool _enableRagdollOnHit = false;

        [Header("Death Settings")]
        [Tooltip("Vật liệu vật lý để áp dụng cho các bộ phận khi chết, tạo độ nảy. Nếu bỏ trống, sẽ không có hiệu ứng nảy.")]
        [SerializeField] private PhysicsMaterial _deathBouncyMaterial;

        [Header("Ragdoll Movement")]
        [Tooltip("Lực để 'lê lết' khi đang ở trạng thái ragdoll. Đặt là 0 để vô hiệu hóa. Hoạt động tốt nhất khi các Rigidbody của ragdoll có giá trị Drag > 0 (ví dụ: 1 hoặc 2).")]
        [SerializeField] private float _ragdollMoveSpeed = 50f;

        [Header("Ragdoll Physics")]
        [Tooltip("Lực hấp dẫn bổ sung tác dụng lên ragdoll để rơi nhanh hơn. Đặt là 0 để dùng trọng lực mặc định.")]
        [SerializeField] private float _extraGravityForce = 20f;

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
        private bool _isFrozen = false; // Cờ để theo dõi trạng thái đóng băng, được cập nhật qua GameEvents

        private Coroutine _recoveryCoroutine;
        private Coroutine _shatterCoroutine;

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

            if (_ragdollRoot != null)
            {
                // SỬA LỖI: Thêm 'true' để tìm cả các component trên các GameObject đang bị tắt trong Prefab.
                // Điều này đảm bảo tất cả các bộ phận của ragdoll đều được quản lý chính xác.
                _ragdollRigidbodies = _ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
                _ragdollColliders = _ragdollRoot.GetComponentsInChildren<Collider>(true);
                
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

        private void OnEnable()
        {
            // Đăng ký lắng nghe các sự kiện hiệu ứng trạng thái để biết khi nào người chơi bị đóng băng.
            GameEvents.OnPlayerStatusEffectApplied += HandleStatusEffectApplied;
            GameEvents.OnPlayerStatusEffectReverted += HandleStatusEffectReverted;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerStatusEffectApplied -= HandleStatusEffectApplied;
            GameEvents.OnPlayerStatusEffectReverted -= HandleStatusEffectReverted;
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
                HandleRagdollGravity();
            }
        }

        #endregion

        #region IExplosionReactable Implementation

        /// <summary>
        /// Xử lý khi bị tác động bởi một vụ nổ.
        /// Quyết định xem có nên kích hoạt ragdoll hay chỉ thêm momentum.
        /// </summary>
        public void OnExplosionHit(Vector3 force, Vector3 point, IBaseBombData bombData)
        {
            // Nếu người chơi đang bị đóng băng (được thông báo qua event), họ sẽ miễn nhiễm với lực vật lý.
            if (_isFrozen)
            {
                // LOGIC ĐÃ SỬA: Nếu bị đóng băng, người chơi không nhận lực đẩy từ vụ nổ.
                // Sát thương vẫn được xử lý bởi PlayerHealth.
                return; // Bỏ qua tất cả các xử lý vật lý.
            }

            // CẢI TIẾN: Nếu người chơi đã ở trạng thái ragdoll, luôn áp dụng lực từ vụ nổ.
            // Bỏ qua các kiểm tra về ngưỡng lực hoặc cài đặt '_enableRagdollOnHit'.
            if (IsRagdollActive)
            {
                EnableRagdollWithForce(force, point);
                return;
            }
            // // LOGIC MỚI: Xử lý các hiệu ứng đặc biệt từ bom.
            // // Nếu bom có hiệu ứng Electrified, luôn kích hoạt ragdoll bất kể lực tác động.
            // if (bombData != null && bombData.Effect == StatusEffectType.Electrified)
            // {
            //     EnableRagdollWithForce(force, point);
            //     return; // Đã xử lý, thoát khỏi phương thức.
            // }

            // Nếu tính năng ragdoll khi bị đánh trúng bị tắt, chỉ thêm momentum và bỏ qua.
            if (!_enableRagdollOnHit)
            {
                _playerController.AddMomentum(force);
                return;
            }
            
            // Nếu lực đủ mạnh, kích hoạt ragdoll.
            if (force.magnitude >= _ragdollForceThreshold)
            {
                EnableRagdollWithForce(force, point);
            }
            // Nếu không, chỉ thêm momentum để đẩy người chơi đi.
            else
            {
                _playerController.AddMomentum(force);
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

            // Nếu đang cố kích hoạt ragdoll trong khi bị đóng băng, hãy bỏ qua.
            if (state && _isFrozen) {
                return;
            }

            IsRagdollActive = state;

            if (state)
            {
                // --- KÍCH HOẠT RAGDOLL ---

                // Lấy vận tốc cuối cùng của PlayerController TRƯỚC KHI nó bị vô hiệu hóa.
                Vector3 lastVelocity = _playerController.GetVelocity();

                // 1. Vô hiệu hóa các component điều khiển
                if (_animator != null) _animator.enabled = false;
                _playerController.enabled = false;
                if (_damageHurtbox != null) _damageHurtbox.enabled = false;
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
                if (_damageHurtbox != null) _damageHurtbox.enabled = true;
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
        /// <param name="killingForce">Lực tùy chọn để áp dụng cho ragdoll khi chết.</param>
        /// <param name="hitPoint">Điểm tác động của lực.</param>
        public void ShatterAndDie(Vector3 killingForce = default, Vector3 hitPoint = default)
        {
            // 1. Kích hoạt trạng thái ragdoll cơ bản.
            if (!IsRagdollActive)
            {
                SetRagdollState(true);
            }

            // CẢI TIẾN: Áp dụng lực khai tử (nếu có) cho TẤT CẢ các bộ phận của ragdoll.
            // Điều này giải quyết vấn đề thi thoảng chỉ có phần thân bị văng đi,
            // tạo ra một hiệu ứng "vỡ tung" mạnh mẽ và đảm bảo tất cả các bộ phận đều nhận lực.
            if (killingForce != default && _ragdollRigidbodies != null)
            {
                foreach (var rb in _ragdollRigidbodies)
                {
                    if (rb != null)
                    {
                        // Sử dụng AddForce để áp dụng lực vào tâm của mỗi bộ phận, tạo ra hiệu ứng văng ra đồng đều.
                        rb.AddForce(killingForce, ForceMode.Impulse);
                    }
                }
            }

            // 2. Áp dụng vật liệu vật lý nảy cho tất cả các collider của ragdoll.
            if (_deathBouncyMaterial != null && _ragdollColliders != null)
            {
                foreach (var col in _ragdollColliders)
                {
                    if (col == null) continue;
                    col.material = _deathBouncyMaterial;
                }
            }

            // 3. Bắt đầu một coroutine để phá hủy các khớp sau một khoảng trễ nhỏ, cho phép lực lan truyền.
            // Mặc dù bây giờ tất cả các bộ phận đều nhận lực trực tiếp, việc giữ lại một frame trễ
            // trước khi phá khớp là một biện pháp an toàn để tránh các xung đột vật lý không mong muốn
            // có thể xảy ra trong cùng một frame.
            if (_shatterCoroutine != null) StopCoroutine(_shatterCoroutine);
            _shatterCoroutine = StartCoroutine(DelayedShatter());
        }

        /// <summary>
        /// Kích hoạt ragdoll và áp dụng một lực ban đầu.
        /// Hữu ích cho các hiệu ứng vụ nổ.
        /// </summary>
        /// <param name="force">Vector lực để áp dụng.</param>
        /// <param name="point">Điểm tác dụng lực.</param>
        public void EnableRagdollWithForce(Vector3 force, Vector3 point)
        {
            // Nếu người chơi đang bị đóng băng, họ sẽ miễn nhiễm với lực vật lý.
            if (_isFrozen)
            {
                return;
            }
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

        /// <summary>
        /// Phá hủy các khớp của ragdoll sau một khoảng trễ ngắn.
        /// Điều này cho phép lực tác động ban đầu được truyền qua các khớp đến toàn bộ cơ thể
        /// trước khi các bộ phận bị tách rời, tạo ra hiệu ứng "văng" tự nhiên hơn.
        /// </summary>
        private IEnumerator DelayedShatter()
        {
            // Chờ đến frame vật lý tiếp theo để đảm bảo lực ban đầu đã được xử lý.
            yield return new WaitForFixedUpdate();

            if (_ragdollRoot != null)
            {
                foreach (var joint in _ragdollRoot.GetComponentsInChildren<Joint>())
                {
                    if (joint != null) // Kiểm tra để chắc chắn khớp chưa bị phá hủy
                        Destroy(joint);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Áp dụng một lực hấp dẫn bổ sung để ragdoll rơi nhanh hơn.
        /// </summary>
        private void HandleRagdollGravity()
        {
            if (_extraGravityForce <= 0f) return;

            foreach (var rb in _ragdollRigidbodies)
            {
                // Chỉ áp dụng lực nếu Rigidbody đang hoạt động (không phải kinematic)
                if (!rb.isKinematic)
                {
                    // Sử dụng ForceMode.Acceleration để không bị ảnh hưởng bởi khối lượng (mass) của bộ phận.
                    // Điều này đảm bảo tất cả các bộ phận rơi với cùng một gia tốc bổ sung.
                    rb.AddForce(Vector3.down * _extraGravityForce, ForceMode.Acceleration);
                }
            }
        }

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

        #region Event Handlers

        /// <summary>
        /// Xử lý khi một hiệu ứng trạng thái được áp dụng lên người chơi.
        /// </summary>
        private void HandleStatusEffectApplied(IPlayer player, StatusEffectType effect)
        {
            // Chỉ phản hồi nếu sự kiện này dành cho chính người chơi này.
            if (player.GameObject != gameObject) return;

            if (effect == StatusEffectType.Frozen)
            {
                _isFrozen = true;
            }
        }

        /// <summary>
        /// Xử lý khi một hiệu ứng trạng thái trên người chơi được hoàn tác.
        /// </summary>
        private void HandleStatusEffectReverted(IPlayer player, StatusEffectType effect)
        {
            // Chỉ phản hồi nếu sự kiện này dành cho chính người chơi này.
            if (player.GameObject != gameObject) return;

            if (effect == StatusEffectType.Frozen)
            {
                _isFrozen = false;
            }
        }
        #endregion
    }
}