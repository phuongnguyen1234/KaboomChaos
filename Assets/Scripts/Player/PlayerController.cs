using UnityEngine;
using Core.Interfaces;
using Core;
using Core.Miscellaneous;

namespace Player
{
    /// <summary>
    /// Lớp Adapter để tích hợp AdvancedWalkerController từ package CMF vào hệ thống của game.
    /// Lớp này kế thừa AdvancedWalkerController, triển khai IPlayer và kết nối với InputSystem của dự án.
    /// </summary>
    [RequireComponent(typeof(Mover))]
    [RequireComponent(typeof(PlayerInputAdapter))] // Thay đổi RequireComponent
    public partial class PlayerController : AdvancedWalkerController, IPlayer
    {
        #region Fields
        [Header("Adapter Settings")]
        [Tooltip("Độ mượt khi xoay nhân vật theo hướng di chuyển ở góc nhìn thứ ba.")]
        [SerializeField] private float _rotationSmoothTime = 0.1f;

        [Header("Floating Text Containers")]
        [SerializeField] private Transform _hpTextContainer;
        [SerializeField] private Transform _coinTextContainer;

        private float _rotationVelocity;

        private CameraController _cameraController;
        private Vector3 _lastActualVelocity; // Vận tốc thực tế từ frame vật lý trước
        private Vector3 _groundAngularVelocity; // Vận tốc góc của mặt đất

        private bool _justLanded; // Cờ báo cho sự kiện tiếp đất
        private bool _isFrozen = false; // Cờ để theo dõi trạng thái đóng băng

        public bool IsFrozen => _isFrozen;

        #endregion
        private float _speedBonus = 0f;
        private float _jumpBonus = 0f;
        private const float BaseSpeed = 1.0f; // Giả định base là 1.0


        #region IPlayer Implementation

        /// <summary>
        /// Tham chiếu đến GameObject của người chơi.
        /// </summary>
        public GameObject GameObject => gameObject;

        /// <summary>
        /// Tốc độ di chuyển hiện tại.
        /// </summary>
        public float CurrentSpeed => movementSpeed;

        /// <summary>
        /// Nhân vật đang di chuyển hay không.
        /// </summary>
        public bool IsMoving => HorizontalSpeed > 0.1f;

        /// <summary>
        /// Nhân vật có đang ở trên mặt đất hay không.
        /// </summary>
        public new bool IsGrounded => base.IsGrounded();

        /// <summary>
        /// Vận tốc hiện tại theo trục Y.
        /// </summary>
        public float VerticalVelocity => GetVelocity().y;

        /// <summary>
        /// Tốc độ di chuyển ngang hiện tại của người chơi.
        /// </summary>
        public float HorizontalSpeed
        {
            get
            {
                // TÍNH TOÁN TỐC ĐỘ TƯƠNG ĐỐI CHO ANIMATION
                // Logic này giải quyết cả hai vấn đề:
                // 1. Nhân vật không chạy animation khi bị kẹt vào tường (world velocity ~ 0).
                // 2. Nhân vật không chạy animation khi đứng yên trên một platform di động (relative velocity ~ 0).

                // SỬA LỖI: Sử dụng vận tốc thực tế từ đầu frame vật lý để animation khớp với chuyển động thực tế.
                Vector3 playerWorldVelocity = _lastActualVelocity;

                // Vận tốc của mặt đất trong world space (được tính trong AdvancedWalkerController).
                Vector3 groundWorldVelocity = groundMomentum;

                // Vận tốc tương đối của player so với mặt đất.
                Vector3 relativeVelocity = playerWorldVelocity - groundWorldVelocity;

                // Chỉ quan tâm đến tốc độ trên mặt phẳng ngang và trả về độ lớn của nó.
                relativeVelocity.y = 0;
                return relativeVelocity.magnitude;
            }
        }

        /// <summary>
        /// Triển khai thuộc tính JustLanded từ IPlayer.
        /// </summary>
        public bool JustLanded
        {
            get
            {
                if (!_justLanded) return false;
                _justLanded = false; // Tự động reset sau khi được đọc
                return true;
            }
        }

        /// <summary>
        /// Triển khai thuộc tính IsClimbing từ IPlayer.
        /// </summary>
        public bool IsClimbing => currentControllerState == ControllerState.Climbing;

        /// <summary>
        /// Triển khai thuộc tính ClimbingSpeed từ IPlayer.
        /// Giá trị này được tính toán trong partial class PlayerController.Climbing.
        /// </summary>
        public float ClimbingSpeed => _climbingDirection;

        /// <summary>
        /// Di chuyển người chơi đến một vị trí mới một cách an toàn.
        /// </summary>
        /// <param name="position">Vị trí thế giới mới.</param>
        /// <param name="rotation">Góc quay thế giới mới (tùy chọn).</param>
        public void Teleport(Vector3 position, Quaternion? rotation = null)
        {
            // Đối với controller CMF, việc thay đổi vị trí phải được thực hiện thông qua component 'Mover'.
            // Việc đặt 'transform.position' trực tiếp sẽ bị ghi đè trong lần cập nhật vật lý tiếp theo.
            if (mover != null)
            {
                mover.SetPosition(position);
                // Reset lại tất cả các lực đang tác động để người chơi không bị "bay" đi sau khi dịch chuyển.
                SetMomentum(Vector3.zero);
            }
            else
            {
                transform.position = position;
            }

            if (rotation.HasValue)
            {
                transform.rotation = rotation.Value;
                _rotationVelocity = 0f;
                if (_cameraController != null)
                {
                    _cameraController.SetCameraRotation(rotation.Value.eulerAngles.y, 0f);
                }
            }
        }

        /// <summary>
        /// Bật hoặc tắt khả năng di chuyển của người chơi.
        /// </summary>
        /// <param name="enabled">True để bật di chuyển, False để tắt.</param>
        public void SetMovementEnabled(bool enabled)
        {
            // Nếu người chơi bị đóng băng, không cho phép bật di chuyển.
            if (_isFrozen && enabled)
            {
                return;
            }
            this.enabled = enabled;
        }

        /// <summary>
        /// Áp dụng một hệ số nhân vào tốc độ di chuyển của người chơi.
        /// </summary>
        public void ApplySpeedMultiplier(float multiplier)
        {
            float bonus = multiplier - 1.0f;
            _speedBonus += bonus;
            _movementSpeed += bonus;
        }

        /// <summary>
        /// Gỡ bỏ một hệ số nhân khỏi tốc độ di chuyển của người chơi.
        /// </summary>
        public void RemoveSpeedMultiplier(float multiplier)
        {
            float bonus = multiplier - 1.0f;
            _speedBonus -= bonus;
            _movementSpeed -= bonus;
        }

        /// <summary>
        /// Áp dụng một hệ số nhân vào lực nhảy của người chơi.
        /// </summary>
        public void ApplyJumpMultiplier(float multiplier)
        {
            float bonus = multiplier - 1.0f;
            _jumpBonus += bonus;
            _jumpForce += bonus; // Giả định _jumpForce là biến được dùng trong Controller
        }

        /// <summary>
        /// Gỡ bỏ một hệ số nhân khỏi lực nhảy của người chơi.
        /// </summary>
        public void RemoveJumpMultiplier(float multiplier)
        {
            float bonus = multiplier - 1.0f;
            _jumpBonus -= bonus;
            _jumpForce -= bonus;
        }
        /// <inheritdoc/>
        public Transform HPTextContainer => _hpTextContainer;

        /// <inheritdoc/>
        public Transform CoinTextContainer => _coinTextContainer;

        #endregion

        #region BGM Control (IPlayer Implementation)

        /// <summary>
        /// <summary>
        /// Reset toàn bộ chỉ số cộng dồn về trạng thái gốc.
        /// </summary>
        public void ResetModifiers()
        {
            _movementSpeed -= _speedBonus;
            _jumpForce -= _jumpBonus;
            _speedBonus = 0f;
            _jumpBonus = 0f;
        }

        #endregion


        #region Public API

        /// <summary>
        /// Tính toán và trả về hướng di chuyển hiện tại dựa trên input của người chơi và hướng camera.
        /// Phương thức này public để các component khác (như RagdollController) có thể sử dụng lại logic này.
        /// </summary>
        /// <returns>Vector3 đã được chuẩn hóa của hướng di chuyển.</returns>
        public Vector3 GetMovementDirection()
        {
            // Gọi phương thức được kế thừa từ AdvancedWalkerController
            return CalculateMovementDirection();
        }

        #endregion

        /// <summary>
        /// Ghi đè hàm Setup để lấy các component và thiết lập Input Actions.
        /// </summary>
        protected override void Setup()
        {
            base.Setup(); // Gọi hàm Setup của lớp cha

            // Gán cameraTransform từ CameraController để di chuyển theo hướng camera
            _cameraController = GetComponent<CameraController>();
            if (_cameraController != null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            
            // Lấy component Collider chính của player để dùng cho các phép tính vật lý
            _mainCollider = GetComponent<Collider>();
        }

        /// <summary>
        /// Được gọi khi component được bật.
        /// Chúng ta sử dụng nó để đảm bảo trạng thái của controller được reset sạch sẽ
        /// mỗi khi nó được kích hoạt lại (ví dụ: sau khi đứng dậy từ ragdoll).
        /// Điều này ngăn chặn các trạng thái cũ (như 'Climbing') gây ra lỗi.
        /// </summary>
        protected void OnEnable()
        {
            // ĐỒNG BỘ HÓA VẬT LÝ:
            // Khi đứng dậy từ ragdoll, RagdollController đã di chuyển transform đến vị trí mới.
            // Tuy nhiên, Rigidbody có thể vẫn "nhớ" vị trí vật lý cũ của nó trước khi bị kinematic.
            // Dòng code này buộc Rigidbody phải cập nhật trạng thái vật lý của nó theo vị trí và góc xoay
            // hiện tại của transform, ngăn chặn việc bị teleport về vị trí cũ.
            if (mover != null) mover.GetComponent<Rigidbody>().position = transform.position;
            
            // Đăng ký lắng nghe sự kiện hiệu ứng trạng thái
            GameEvents.OnPlayerStatusEffectApplied += HandleStatusEffectApplied;
            GameEvents.OnPlayerStatusEffectReverted += HandleStatusEffectReverted;

            // Đảm bảo trạng thái đóng băng được reset khi bật lại.
            // Điều này quan trọng nếu player bị đóng băng và sau đó bị vô hiệu hóa/kích hoạt lại.
            _isFrozen = false;

            ResetStateToFalling();
            SetMomentum(Vector3.zero);

        }

        protected void OnDisable()
        {
            GameEvents.OnPlayerStatusEffectApplied -= HandleStatusEffectApplied;
            GameEvents.OnPlayerStatusEffectReverted -= HandleStatusEffectReverted;
        }

        /// <summary>
        /// Ghi đè phương thức OnJumpStart để tùy chỉnh hành vi nhảy.
        /// 1. Vô hiệu hóa 'jumpInputIsLocked' để cho phép Bunny Hop (nhảy liên tục khi giữ phím).
        /// 2. Vô hiệu hóa 'jumpDuration' bằng cách chuyển trạng thái ngay lập tức, làm cho cú nhảy có chiều cao cố định.
        /// </summary>
        protected override void OnJumpStart()
        {
            // Gọi phương thức gốc để áp dụng lực nhảy và các sự kiện cơ bản.
            // Tuy nhiên, phương thức gốc sẽ khóa input nhảy, chúng ta sẽ không gọi nó.
            // base.OnJumpStart(); 

            // Tái triển khai logic của OnJumpStart nhưng bỏ qua phần khóa input.
            if (_useLocalMomentum)
                momentum = tr.localToWorldMatrix * momentum;

            momentum += tr.up * _jumpForce;

            // Gán lại thời gian bắt đầu nhảy. Điều này rất quan trọng để logic trong
            // DetermineControllerState hoạt động đúng (ví dụ: hết thời gian _jumpDuration).
            currentJumpStartTime = Time.time;

            OnJump?.Invoke(momentum);

            if (_useLocalMomentum)
                momentum = tr.worldToLocalMatrix * momentum;

            // Thay vì ép trạng thái thành 'Rising', hãy để lớp cha xử lý việc chuyển sang 'Jumping'.
            // Điều này cho phép trạng thái 'Grounded' được thiết lập đúng cách trong một frame trước khi nhảy lại.
            // Bằng cách không gọi base.OnJumpStart(), chúng ta vẫn bỏ qua được việc khóa input ('jumpInputIsLocked = true'),
            // cho phép thực hiện bunny hop.
            // Dòng dưới đây không còn cần thiết và là nguyên nhân gây ra vấn đề.
            // currentControllerState = ControllerState.Rising;
        }

        /// <summary>
        /// Ghi đè hàm OnGroundContactRegained để bật cờ _justLanded.
        /// </summary>
        protected override void OnGroundContactRegained()
        {
            base.OnGroundContactRegained();
            _justLanded = true;
        }


        /// <summary>
        /// Ghi đè FixedUpdate của lớp cha để thêm logic xoay nhân vật.
        /// Luôn phải gọi base.FixedUpdate() để đảm bảo logic di chuyển và vật lý gốc được thực thi.
        /// </summary>
        protected override void FixedUpdate()
        {
            // LƯU Ý QUAN TRỌNG VỀ THỨ TỰ THỰC THI:
            // Lấy vận tốc của Rigidbody ở ĐẦU FixedUpdate. Đây là vận tốc cuối cùng sau khi vật lý được tính ở frame trước.
            if (mover != null)
            {
                _lastActualVelocity = mover.GetVelocity();
            }

            // Lấy vận tốc góc của mặt đất để xử lý các platform xoay.
            _groundAngularVelocity = Vector3.zero;
            if (base.IsGrounded())
            {
                Collider groundCollider = mover.GetGroundCollider();
                if (groundCollider != null && groundCollider.attachedRigidbody != null)
                {
                    _groundAngularVelocity = groundCollider.attachedRigidbody.angularVelocity;
                }
            }
            
            ClimbingUpdate(); // Gọi logic cập nhật của phần leo trèo.
            base.FixedUpdate(); // Rất quan trọng! Gọi hàm của lớp cha để xử lý di chuyển.
            HandleCharacterRotation();
        }

        /// <summary>
        /// Xử lý việc xoay nhân vật.
        /// - Ở chế độ First Person/Shift Lock: Xoay tức thì theo camera (logic này nằm trong CameraController).
        /// - Ở chế độ Third Person (mặc định): Xoay mượt mà theo hướng di chuyển.
        /// </summary>
        private void HandleCharacterRotation()
        {
            // Nếu người chơi đang bị đóng băng, không cho phép xoay.
            if (_isFrozen)
            {
                return;
            }

            if (_cameraController == null) return;

            bool isShiftLocked = _cameraController.IsShiftLock;

            // Khi bật Shift Lock hoặc ở góc nhìn thứ nhất, nhân vật sẽ xoay theo camera.
            // Logic này được xử lý trong CameraController để đảm bảo thứ tự thực thi đúng.
            if (isShiftLocked || _cameraController.IsFirstPerson)
            {
                // Reset vận tốc xoay mượt để không bị giật khi chuyển về góc nhìn thứ 3.
                _rotationVelocity = 0f;
                // Xử lý xoay nhân vật theo camera đã được chuyển sang CameraController.
            }
            else
            {
                if (currentControllerState == ControllerState.Climbing)
                {
                    return;
                }

                // CẢI TIẾN: Tách biệt logic xoay khi có input và khi đứng yên trên platform động.
                bool hasMovementInput = GetMovementDirection().sqrMagnitude > 0.01f;

                if (hasMovementInput)
                {
                    Vector3 lookDirection;

                    // Khi ở trên không, xoay theo hướng input để có cảm giác điều khiển linh hoạt.
                    if (!IsGrounded())
                    {
                        lookDirection = GetMovementDirection();
                    }
                    // Khi ở trên mặt đất, xoay theo hướng di chuyển thực tế để xử lý trượt tường.
                    else
                    {
                        lookDirection = _lastActualVelocity;
                        lookDirection.y = 0;
                    }

                    // Chỉ xoay khi có hướng nhìn hợp lệ.
                    if (lookDirection.sqrMagnitude > 0.01f)
                    {
                        float targetAngle = Mathf.Atan2(lookDirection.x, lookDirection.z) * Mathf.Rad2Deg;
                        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationVelocity, _rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, angle, 0f);
                    }
                }
                else
                {
                    // KHI KHÔNG CÓ INPUT: Xoay theo platform nếu nó đang quay.
                    // Điều này giải quyết vấn đề đứng trên đĩa xoay mà không quay mặt theo.
                    if (IsGrounded() && _groundAngularVelocity.sqrMagnitude > 0.01f)
                    {
                        // CHỈ XOAY TRỤC Y: Chỉ lấy thành phần xoay quanh trục Y của platform
                        // để đảm bảo người chơi luôn đứng thẳng và không bị nghiêng theo platform.
                        Vector3 yawRotation = new(0, _groundAngularVelocity.y, 0);
                        Quaternion rotationDelta = Quaternion.Euler(Mathf.Rad2Deg * Time.fixedDeltaTime * yawRotation);
                        transform.rotation = rotationDelta * transform.rotation;
                    }
                }
            }
        }

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

                // --- TỐC ĐỘ LEO TỶ LỆ VỚI TỐC ĐỘ DI CHUYỂN ---
                // Tính hệ số tốc độ hiện tại so với tốc độ cơ bản. Các skill/perk (SpeedRunner, SuperDash...)
                // tăng _movementSpeed thông qua _speedBonus, nen chi so nay phan anh dung he so hien tai.
                // Vi du: _speedBonus = +50% => base = _movementSpeed - _speedBonus, ratio = 1.5.
                float baseMovementSpeed = _movementSpeed - _speedBonus;
                float speedRatio = baseMovementSpeed > 0f ? Mathf.Max(0f, _movementSpeed / baseMovementSpeed) : 1f;

                // Tai thoi diem sap ap dung toc do leo, tu dong tai tinh _movementSpeed (vi no co the da duoc boi).
                // Ap dung toc do leo co ti le voi toc do di chuyen hien tai.
                momentum = climbVelocity * _climbSpeed * speedRatio;
            }
            else
            {
                // LOGIC BĂNG CHUYỀN (CONVEYOR BELT):
                // Ghi đè groundMomentum nếu đang đứng trên băng chuyền.
                // Phải thực hiện trước khi gọi base.HandleMomentum() để nó sử dụng giá trị mới.
                if (currentControllerState == ControllerState.Grounded)
                {
                    if (mover.GetGroundCollider() != null && mover.GetGroundCollider().TryGetComponent<ConveyorBelt>(out var belt))
                    {
                        // Ghi đè groundMomentum với vận tốc của băng chuyền.
                        // Bộ điều khiển gốc (AdvancedWalkerController) sẽ sử dụng giá trị này trong base.HandleMomentum()
                        // để áp dụng ma sát và di chuyển người chơi một cách chính xác.
                        groundMomentum = belt.WorldVelocity;
                    }
                }

                // Nếu không leo, sử dụng logic của lớp cha
                base.HandleMomentum();

                 // --- LOGIC CHỐNG DÍNH TƯỜNG (WALL STICK PREVENTION) ---
                // Khi người chơi nhảy vào tường và giữ input di chuyển, họ có thể bị "dính" lại do ma sát.
                // Logic này sẽ loại bỏ phần vận tốc hướng vào tường khi người chơi ở trên không,
                // cho phép họ trượt dọc theo tường một cách tự nhiên.

                // Chỉ áp dụng khi ở trên không.
                if (!IsGrounded)
                {
                    Vector3 horizontalMomentum = momentum;
                    horizontalMomentum.y = 0;

                    // Chỉ kiểm tra khi có vận tốc ngang (người chơi đang cố di chuyển trên không).
                    if (horizontalMomentum.magnitude > 0.01f)
                    {
                        var capsule = _mainCollider as CapsuleCollider; // Lấy capsule collider chính.
                        if (capsule != null) // Đảm bảo có collider để lấy thông số.
                        {
                            // CẢI TIẾN: Sử dụng SphereCast thay vì Raycast.
                            // Raycast quá chính xác và có thể "trượt" ở các góc nhọn, gây ra lỗi ma sát mà bạn gặp phải.
                            // SphereCast có thể tích, đại diện cho chiều rộng của người chơi tốt hơn, giúp phát hiện va chạm đáng tin cậy ở mọi góc độ.
                            float castRadius = capsule.radius * 0.9f; // Dùng bán kính nhỏ hơn một chút để tránh dương tính giả với mặt đất.
                            float castDistance = 0.2f; // Một khoảng cách ngắn là đủ để phát hiện tường đang tiếp xúc.
                            Vector3 castOrigin = transform.position + capsule.center;

                            // Bắn một SphereCast theo hướng di chuyển ngang.
                            if (Physics.SphereCast(castOrigin, castRadius, horizontalMomentum.normalized, out RaycastHit hit, castDistance, ~0, QueryTriggerInteraction.Ignore))
                            {
                                // Chỉ xử lý nếu va chạm với một bức tường (bề mặt gần như thẳng đứng).
                                if (Mathf.Abs(hit.normal.y) < 0.707f) // Ngưỡng 45 độ.
                                {
                                    // Chiếu vận tốc ngang lên mặt phẳng của tường để loại bỏ lực đẩy vào tường.
                                    Vector3 projectedHorizontal = Vector3.ProjectOnPlane(horizontalMomentum, hit.normal);
                                    momentum = new Vector3(projectedHorizontal.x, momentum.y, projectedHorizontal.z);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}