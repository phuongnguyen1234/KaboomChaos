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

        private bool _justLanded; // Cờ báo cho sự kiện tiếp đất

        #endregion

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

                // Vận tốc của player trong world space.
                Vector3 playerWorldVelocity = mover.GetVelocity();

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
        protected virtual void OnEnable()
        {
            // ĐỒNG BỘ HÓA VẬT LÝ:
            // Khi đứng dậy từ ragdoll, RagdollController đã di chuyển transform đến vị trí mới.
            // Tuy nhiên, Rigidbody có thể vẫn "nhớ" vị trí vật lý cũ của nó trước khi bị kinematic.
            // Dòng code này buộc Rigidbody phải cập nhật trạng thái vật lý của nó theo vị trí và góc xoay
            // hiện tại của transform, ngăn chặn việc bị teleport về vị trí cũ.
            mover.GetComponent<Rigidbody>().position = transform.position;

            ResetStateToFalling();
            SetMomentum(Vector3.zero);
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