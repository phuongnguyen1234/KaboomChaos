using UnityEngine;
using Core.Interfaces;

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

        private float _rotationVelocity;

        private CameraController _cameraController;
        private PlayerAnimator _playerAnimator; // Tham chiếu đến PlayerAnimator

        private bool _justLanded; // Cờ báo cho sự kiện tiếp đất

        #endregion

        #region IPlayer Implementation

        /// <summary>
        /// Tham chiếu đến GameObject của người chơi.
        /// </summary>
        public new GameObject gameObject => base.gameObject;

        /// <summary>
        /// Tốc độ di chuyển hiện tại.
        /// </summary>
        public float CurrentSpeed => movementSpeed;

        /// <summary>
        /// Nhân vật đang di chuyển hay không.
        /// </summary>
        public bool IsMoving => GetMovementVelocity().magnitude > 0.1f;

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
        public float HorizontalSpeed => GetMovementVelocity().magnitude;

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
            
            // Lấy component PlayerAnimator
            _playerAnimator = GetComponent<PlayerAnimator>();

            // Lấy component Collider chính của player để dùng cho các phép tính vật lý
            _mainCollider = GetComponent<Collider>();
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

            momentum += tr.up * _jumpSpeed;

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
                // Khi đang leo và không bật Shift Lock, không xoay nhân vật theo hướng di chuyển.
                if (currentControllerState == ControllerState.Climbing)
                {
                    return;
                }

                // Ở góc nhìn thứ 3 (không Shift Lock), xoay nhân vật mượt mà theo hướng di chuyển.
                Vector3 horizontalVelocity = GetMovementVelocity();
                horizontalVelocity.y = 0;

                // Chỉ xoay khi có di chuyển
                if (horizontalVelocity.magnitude < 0.1f) return;

                // Tính toán góc xoay dựa trên hướng di chuyển
                float targetAngle = Mathf.Atan2(horizontalVelocity.x, horizontalVelocity.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationVelocity, _rotationSmoothTime);
                
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }
    }
}