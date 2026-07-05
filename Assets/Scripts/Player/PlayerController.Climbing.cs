using UnityEngine;

namespace Player
{
    /// <summary>
    /// Partial class chứa logic cho cơ chế leo trèo của Player.
    /// </summary>
    public partial class PlayerController
    {
        #region Climbing Fields

        [Header("Climbing Settings")]
        [Tooltip("Tag của các vật thể có thể leo được.")]
        [SerializeField] private string _climbableTag = "Climbable";
        [Tooltip("Tốc độ leo của nhân vật.")]
        [SerializeField] private float _climbSpeed = 4f;
        [Tooltip("Góc tối đa so với bề mặt tường để có thể bắt đầu leo.")]
        [SerializeField] private float _maxClimbAngle = 30f;
        [Tooltip("Lực đẩy người chơi ra khỏi tường khi nhảy.")]
        [SerializeField] private float _climbJumpForce = 5f;
        [Tooltip("Chiều cao phần hình trụ của capsule (khoảng cách giữa 2 tâm hình cầu).")]
        [SerializeField] private float _climbCapsuleHeight = 1.5f;
        [Tooltip("Độ cao của tâm capsule dò tìm so với gốc của player.")]
        [SerializeField] private float _climbCapsuleCenterY = 1.0f;
        [Tooltip("Khoảng cách SphereCast sẽ bắn ra để tìm tường leo.")]
        [SerializeField] private float _climbDetectionDistance = 1f;
        [Tooltip("Bán kính của SphereCast dùng để phát hiện tường.")]
        [SerializeField] private float _climbDetectionRadius = 0.4f;

        private Collider _mainCollider; // Collider chính của Player, dùng để tính toán va chạm
        private bool _canClimb; // Cờ cho biết có tường leo được ở phía trước không
        private Vector3 _climbableSurfaceNormal; // Normal của bề mặt tường leo
        private float _climbingDirection; // Hướng leo hiện tại (-1 là xuống, 1 là lên, 0 là đứng yên)

        #endregion

        #region Climbing Logic

        /// <summary>
        /// Cập nhật logic dành riêng cho việc leo trèo. Được gọi từ FixedUpdate chính.
        /// </summary>
        private void ClimbingUpdate()
        {
            // Luôn kiểm tra xem có thể leo được không, bất kể trạng thái hiện tại
            CheckForClimbableSurface();
        }

        /// <summary>
        /// Sử dụng SphereCast để kiểm tra xem có bề mặt leo được ở phía trước không.
        /// </summary>
        private void CheckForClimbableSurface()
        {
            _canClimb = false; // Reset cờ ở mỗi frame

            Vector3 castDirection;

            // Nếu đang leo, luôn kiểm tra phía trước mặt (hướng vào tường)
            if (currentControllerState == ControllerState.Climbing)
            {
                // Nếu normal của tường hợp lệ, cast vào đó để kiểm tra tường còn đó không.
                castDirection = (_climbableSurfaceNormal != Vector3.zero) ? -_climbableSurfaceNormal : tr.forward;
            }
            // Nếu không leo, luôn kiểm tra theo hướng player đang nhìn.
            // Điều này cho phép phát hiện tường ngay cả khi đứng yên.
            else
            {
                castDirection = tr.forward;
            }

            // Tính toán 2 điểm của capsule cho việc cast
            // Logic mới: Coi _climbCapsuleHeight là khoảng cách giữa hai tâm của hình cầu (chiều cao phần hình trụ).
            // Capsule sẽ được đặt ở trung tâm người chơi (mặc định ở độ cao 1m).
            
            // Trung tâm của capsule dò tìm, đặt ở độ cao 1m so với gốc của player.
            Vector3 capsuleCenter = tr.position + Vector3.up * _climbCapsuleCenterY;
            
            float halfCylinderHeight = _climbCapsuleHeight / 2f;

            Vector3 p1 = capsuleCenter + Vector3.up * halfCylinderHeight; // Điểm trên của capsule
            Vector3 p2 = capsuleCenter - Vector3.up * halfCylinderHeight; // Điểm dưới của capsule

            // 1. Bắn một CapsuleCast để phát hiện tường ở phía trước
            if (Physics.CapsuleCast(p1, p2, _climbDetectionRadius, castDirection, out RaycastHit hit, _climbDetectionDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                // Kiểm tra xem vật thể có tag "Climbable" không
                if (hit.collider.CompareTag(_climbableTag))
                {
                    _canClimb = true;
                    _climbableSurfaceNormal = hit.normal;
                    return; // Tìm thấy tường, không cần kiểm tra thêm
                }
            }

            // 2. Nếu không phát hiện bằng cast (có thể do đã ở quá sát), dùng OverlapCapsule
            // Chỉ thực hiện kiểm tra này khi không leo trèo để tránh các vấn đề không mong muốn
            if (currentControllerState != ControllerState.Climbing && _mainCollider != null)
            {
                Collider[] overlaps = Physics.OverlapCapsule(p1, p2, _climbDetectionRadius, ~0, QueryTriggerInteraction.Ignore);
                foreach (var overlapCollider in overlaps)
                {
                    // Bỏ qua chính collider của player
                    if (overlapCollider == _mainCollider) continue;

                    if (overlapCollider.CompareTag(_climbableTag))
                    {
                        // Đã tìm thấy một bức tường có thể leo được mà chúng ta đang tiếp xúc.
                        // Dùng ComputePenetration để có được hướng pháp tuyến (normal) chính xác.
                        if (Physics.ComputePenetration(
                            _mainCollider, tr.position, tr.rotation,
                            overlapCollider, overlapCollider.transform.position, overlapCollider.transform.rotation,
                            out Vector3 penetrationDirection, out float penetrationDistance))
                        {
                            _canClimb = true;
                            _climbableSurfaceNormal = -penetrationDirection; // Pháp tuyến là hướng ngược lại của hướng đẩy ra
                            break; // Thoát khỏi vòng lặp khi đã tìm thấy tường
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ghi đè để thêm logic chuyển đổi trạng thái Climbing.
        /// </summary>
        protected override ControllerState DetermineControllerState()
        {
            // Nếu đang leo trèo
            if (currentControllerState == ControllerState.Climbing)
            {
                // Tính góc giữa hướng nhìn của player và hướng vào tường
                float angleToWall = Vector3.Angle(tr.forward, -_climbableSurfaceNormal);

                // Điều kiện để thoát trạng thái leo:
                // 1. Không còn tường để leo.
                // 2. Người chơi nhấn nhảy.
                // 3. Người chơi quay mặt ra hướng khác (vượt quá góc cho phép).
                if (!_canClimb || jumpKeyWasPressed || angleToWall > _maxClimbAngle)
                {
                    // Nếu nhảy, đẩy người chơi ra khỏi tường và lên trên.
                    if (jumpKeyWasPressed)
                    {
                        // Kết hợp lực đẩy ra khỏi tường (_climbJumpForce) và lực nhảy lên trên (_jumpSpeed).
                        // Phải thực hiện trước khi reset normal.
                        Vector3 jumpOffMomentum = (_climbableSurfaceNormal * _climbJumpForce) + (tr.up * _jumpSpeed);
                        AddMomentum(jumpOffMomentum);
                    }

                    // Reset normal của tường khi thoát trạng thái leo
                    _climbableSurfaceNormal = Vector3.zero;
                    currentControllerState = ControllerState.Falling;
                    return ControllerState.Falling;
                }

                // Cải tiến 1: Nếu đang leo xuống và chạm đất, thoát trạng thái leo.
                bool isGroundedNow = mover.IsGrounded(); // Sử dụng mover.IsGrounded() để có kết quả chính xác

                if (isGroundedNow) // Chỉ cần chạm đất là thoát
                    return ControllerState.Grounded;

                return ControllerState.Climbing;
            }

            // Điều kiện để bắt đầu leo:
            // 1. Có thể leo (đã phát hiện tường).
            // 2. Đang di chuyển về phía tường.
            // 3. Hướng nhìn của nhân vật cũng phải hướng vào tường.
            Vector3 moveDirection = CalculateMovementDirection();
            float moveAngleToWall = Vector3.Angle(moveDirection, -_climbableSurfaceNormal);
            float facingAngleToWall = Vector3.Angle(tr.forward, -_climbableSurfaceNormal);

            // Bỏ điều kiện '!IsGrounded()' để cho phép bắt đầu leo từ mặt đất.
            // Thêm điều kiện moveDirection.magnitude > 0.1f để đảm bảo người chơi có chủ ý di chuyển.
            if (_canClimb && moveDirection.magnitude > 0.1f && moveAngleToWall < _maxClimbAngle && facingAngleToWall < _maxClimbAngle)
            {
                // Xóa vận tốc cũ để bám vào tường
                SetMomentum(Vector3.zero);
                return ControllerState.Climbing;
            }

            // Nếu không, sử dụng logic của lớp cha
            return base.DetermineControllerState();
        }

        /// <summary>
        /// Ghi đè để thêm logic di chuyển khi đang leo.
        /// </summary>
        protected override void HandleMomentum()
        {
            if (currentControllerState == ControllerState.Climbing)
            {
                // Xóa hết vận tốc hiện tại để không bị trượt/rơi
                momentum = Vector3.zero;

                // Lấy hướng di chuyển của người chơi
                Vector3 moveDirection = CalculateMovementDirection();

                _climbingDirection = 0f;

                // Chỉ tính toán hướng leo khi có input di chuyển
                if (moveDirection.magnitude > 0.1f)
                {
                    // --- LOGIC TỔNG QUÁT HÓA ---
                    // Logic này hoạt động cho mọi góc camera.
                    // Định nghĩa "leo xuống" là bất kỳ di chuyển nào có ý định rõ ràng là "hướng ra khỏi tường".
                    // Mọi di chuyển khác (vào tường, song song tường) đều được hiểu là "leo lên".

                    // Tính toán thành phần "hướng ra khỏi tường" của vector di chuyển.
                    float awayComponent = Vector3.Dot(moveDirection.normalized, _climbableSurfaceNormal);

                    // Ngưỡng để xác định ý định "leo xuống".
                    // Giá trị 0.5f tương ứng với việc người chơi phải di chuyển trong một góc 60 độ so với hướng ra khỏi tường.
                    const float climbDownThreshold = 0.5f;

                    if (awayComponent > climbDownThreshold)
                    {
                        _climbingDirection = -1f; // Leo xuống
                    }
                    else
                    {
                        _climbingDirection = 1f; // Leo lên
                    }
                }

                // Tạo vận tốc leo trên mặt phẳng tường
                Vector3 climbVelocity = Vector3.ProjectOnPlane(tr.up, _climbableSurfaceNormal).normalized * _climbingDirection;

                // Áp dụng vận tốc leo
                momentum = climbVelocity * _climbSpeed;
            }
            else
            {
                // Nếu không leo, sử dụng logic của lớp cha
                base.HandleMomentum();
            }
        }

        #endregion

        #region Gizmos

        /// <summary>
        /// Vẽ Gizmos trong Editor khi đối tượng được chọn để trực quan hóa vùng phát hiện leo trèo.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Đảm bảo 'tr' được gán giá trị ngay cả trong Editor Mode,
            // vì Awake() không được gọi.
            if (tr == null) tr = transform;

            Vector3 castDirection;

            // Logic để xác định hướng cast phải khớp với CheckForClimbableSurface
            if (Application.isPlaying && currentControllerState == ControllerState.Climbing)
            {
                // Nếu đang leo, cast vào tường
                castDirection = (_climbableSurfaceNormal != Vector3.zero) ? -_climbableSurfaceNormal : tr.forward;
            }
            else
            {
                // Nếu không, cast về phía trước mặt player
                castDirection = transform.forward;
            }

            // Tính toán vị trí của capsule để vẽ Gizmos
            // Logic vẽ phải khớp hoàn toàn với logic tính toán trong CheckForClimbableSurface.
            
            // Trung tâm của capsule dò tìm, đặt ở độ cao 1m so với gốc của player.
            Vector3 capsuleCenter = tr.position + Vector3.up * _climbCapsuleCenterY;
            
            float halfCylinderHeight = _climbCapsuleHeight / 2f;

            Vector3 p1 = capsuleCenter + Vector3.up * halfCylinderHeight; // Điểm trên của capsule
            Vector3 p2 = capsuleCenter - Vector3.up * halfCylinderHeight; // Điểm dưới của capsule

            Vector3 castVector = castDirection.normalized * _climbDetectionDistance;

            // Thay đổi màu sắc dựa trên trạng thái phát hiện.
            // Chỉ hoạt động khi game đang chạy vì cờ _canClimb được cập nhật trong FixedUpdate.
            Gizmos.color = Application.isPlaying && _canClimb ? Color.green : Color.yellow;

            // Vẽ capsule ở vị trí bắt đầu
            DrawWireCapsule(p1, p2, _climbDetectionRadius);

            // Vẽ capsule ở vị trí kết thúc
            DrawWireCapsule(p1 + castVector, p2 + castVector, _climbDetectionRadius);
        }

        // Hàm tiện ích để vẽ một capsule trong Gizmos
        private void DrawWireCapsule(Vector3 p1, Vector3 p2, float radius)
        {
            Gizmos.DrawWireSphere(p1, radius);
            Gizmos.DrawWireSphere(p2, radius);
            Gizmos.DrawLine(p1 + transform.right * radius, p2 + transform.right * radius);
            Gizmos.DrawLine(p1 - transform.right * radius, p2 - transform.right * radius);
            Gizmos.DrawLine(p1 + transform.forward * radius, p2 + transform.forward * radius);
            Gizmos.DrawLine(p1 - transform.forward * radius, p2 - transform.forward * radius);
        }
        #endregion
    }
}
