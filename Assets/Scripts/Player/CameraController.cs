using UnityEngine;
using Unity.Cinemachine;
using Core.Interfaces;
using UnityEngine.InputSystem;

namespace Player{
public class CameraController : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("Kéo Camera Góc nhìn thứ 3 (Orbital Follow) vào đây. Nếu bỏ trống, script sẽ tự tìm theo tên 'ThirdPersonCamera'")]
    public CinemachineCamera thirdPersonCamera;
    [Tooltip("Kéo Camera Góc nhìn thứ nhất (Pan Tilt) vào đây. Nếu bỏ trống, script sẽ tự tìm theo tên 'FirstPersonCamera'")]
    public CinemachineCamera firstPersonCamera;

    [Header("Camera Targets")]
    [Tooltip("Điểm camera góc nhìn thứ 3 sẽ nhìn vào (LookAt). Nếu bỏ trống, sẽ dùng Follow target.")]
    public Transform thirdPersonLookAtTarget;
    [Tooltip("Điểm camera góc nhìn thứ 1 sẽ được đặt tại đó (Follow). Nếu bỏ trống, sẽ dùng transform của Player.")]
    public Transform firstPersonFollowTarget;

    [Header("Rotation Settings")]
    public float lookSpeed = 0.5f;
    public bool invertY = true;

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    [Header("First Person Settings")]
    public bool enableFirstPerson = true;
    public float firstPersonThreshold = 1f;
    public Renderer[] playerRenderers;
    private bool isFirstPerson = false;
    
    [Header("Shift Lock Settings")]
    public bool enableShiftLock = true;
    [Tooltip("Điểm neo cho camera khi bật Shift Lock. Camera sẽ Follow và LookAt điểm này.")]
    public Transform shiftLockTarget;
    private bool isShiftLock = false;

    [Header("Input Settings")]
    [Tooltip("Kéo Action tương ứng với nút Shift Lock vào đây")]
    public InputActionReference shiftLockAction;

    // Components
    private CinemachineThirdPersonFollow thirdPersonFollow;
    private CinemachineOrbitalFollow orbitalFollow;
    private CinemachinePanTilt firstPersonPanTilt;

    private PlayerController _playerController;
    private RagdollController _ragdollController;
    private Transform _playerTransform;

    // Lưu target mặc định để có thể reset sau khi ragdoll
    private Transform _defaultFollowTarget;
    private Transform _defaultLookAtTarget;
    private Transform _defaultFirstPersonFollowTarget;
    private IUIManager _uiManager;
    private Transform _overrideFollowTarget; // Mục tiêu tạm thời, ví dụ như khi ragdoll

    public bool IsFirstPerson => isFirstPerson;
    public bool IsShiftLock => isShiftLock;

    void Awake()
    {
        // 1. Tự động tìm Camera nếu chưa gán
        if (thirdPersonCamera == null)
        {
            GameObject camObj = GameObject.Find("ThirdPersonCamera");
            if (camObj != null) thirdPersonCamera = camObj.GetComponent<CinemachineCamera>();
        }
        if (firstPersonCamera == null)
        {
            GameObject camObj = GameObject.Find("FirstPersonCamera");
            if (camObj != null) firstPersonCamera = camObj.GetComponent<CinemachineCamera>();
        }

        // 2. Gán Target cho Camera là chính bản thân Player (biến transform của script này)
        _playerTransform = this.transform; // Lưu lại transform của player
        _playerController = GetComponent<PlayerController>(); // Lấy PlayerController
        _ragdollController = GetComponent<RagdollController>(); // Lấy RagdollController

        // Gán target mặc định cho LookAt là chính player, phòng trường hợp không có target nào được gán
        Transform thirdPersonTarget = thirdPersonLookAtTarget != null ? thirdPersonLookAtTarget : _playerTransform;
        Transform followTarget = firstPersonFollowTarget != null ? firstPersonFollowTarget : _playerTransform;

        // 3. Gán Target cho Camera
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.Follow = thirdPersonTarget;
            thirdPersonCamera.LookAt = thirdPersonTarget;

            // Lưu lại target mặc định để có thể reset sau khi ragdoll
            _defaultFollowTarget = thirdPersonCamera.Follow;
            _defaultLookAtTarget = thirdPersonCamera.LookAt;
        }
        if (firstPersonCamera != null)
        {
            firstPersonCamera.Follow = followTarget;
            _defaultFirstPersonFollowTarget = firstPersonCamera.Follow;
        }

        // 3. Tìm các component điều khiển
        if (thirdPersonCamera != null)
        {
            thirdPersonFollow = thirdPersonCamera.GetComponent<CinemachineThirdPersonFollow>();
            orbitalFollow = thirdPersonCamera.GetComponent<CinemachineOrbitalFollow>();
        }

        if (firstPersonCamera != null)
        {
            firstPersonPanTilt = firstPersonCamera.GetComponent<CinemachinePanTilt>();
        }

        // Cài đặt ưu tiên ban đầu
        SetFirstPersonMode(false);
        UpdateCrosshairs();

        // Lấy instance của UI Manager
        _uiManager = IUIManager.Instance;

    }

    void Update()
    {
        if (Mouse.current == null) return;

        // Xử lý bật/tắt Shift Lock
        if (enableShiftLock && shiftLockAction != null && shiftLockAction.action.WasPressedThisFrame())
        {
            isShiftLock = !isShiftLock;
            UpdateCrosshairs();
            UpdateCameraTargets();
        }

        HandleRotation();
        HandleZoom();

        // Luôn cập nhật trạng thái con trỏ ở cuối mỗi frame để đảm bảo tính nhất quán.
        UpdateCursorState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Khi game lấy lại focus, buộc cập nhật lại trạng thái con trỏ để tránh bị kẹt.
        if (hasFocus)
            UpdateCursorState();
    }
    
    // Sử dụng LateUpdate để xoay nhân vật theo camera sau khi tất cả các tính toán di chuyển đã hoàn tất.
    // Điều này đảm bảo camera và nhân vật luôn đồng bộ.
    void LateUpdate()
    {
        HandleCharacterRotationWithCamera();
    }

    #region Public Methods for Ragdoll

    /// <summary>
    /// Chuyển mục tiêu theo dõi của camera sang một transform khác (ví dụ: hông hoặc đầu của ragdoll).
    /// </summary>
    /// <param name="newTarget">Transform mới để camera theo dõi và nhìn vào.</param>
    public void SetFollowTarget(Transform newTarget)
    {
        _overrideFollowTarget = newTarget;
        // Khi ragdoll, luôn ẩn crosshair
        _uiManager?.SetShiftLockCrosshair(false); // Ẩn crosshair shift lock
        _uiManager?.SetFirstPersonCrosshair(false); // Ẩn crosshair góc nhìn thứ nhất
        UpdateCameraTargets();
    }

    /// <summary>
    /// Xóa mục tiêu tạm thời và yêu cầu camera cập nhật lại mục tiêu của nó.
    /// Được gọi khi trạng thái ragdoll kết thúc.
    /// </summary>
    public void ResetFollowTarget()
    {
        _overrideFollowTarget = null;
        UpdateCrosshairs();
        UpdateCameraTargets();
    }

    #endregion


    /// <summary>
    /// Cập nhật mục tiêu camera chính xác dựa trên các trạng thái hiện tại (FirstPerson, Shift Lock, Ragdoll).
    /// </summary>
    private void UpdateCameraTargets()
    {
        if (thirdPersonCamera == null) return;

        // Ưu tiên 1: Shift Lock
        if (isShiftLock && shiftLockTarget != null)
        {
            thirdPersonCamera.Follow = shiftLockTarget;
            thirdPersonCamera.LookAt = shiftLockTarget;
        }
        // Ưu tiên 2: Mục tiêu tạm thời (Ragdoll)
        else if (_overrideFollowTarget != null)
        {
            thirdPersonCamera.Follow = _overrideFollowTarget;
            thirdPersonCamera.LookAt = _overrideFollowTarget;
        }
        // Mặc định: Mục tiêu ban đầu
        else
        {
            thirdPersonCamera.Follow = _defaultFollowTarget;
            thirdPersonCamera.LookAt = _defaultLookAtTarget;
        }

        // Handle First Person Camera
        if (firstPersonCamera != null)
        {
            // In first person, the only override is ragdoll.
            firstPersonCamera.Follow = _overrideFollowTarget != null 
                ? _overrideFollowTarget 
                : _defaultFirstPersonFollowTarget;
        }
    }

    /// <summary>
    /// Cập nhật trạng thái hiển thị của các crosshair dựa trên chế độ camera hiện tại.
    /// </summary>
    private void UpdateCrosshairs()
    {
        if (_uiManager == null) return;

        // Logic mới:
        // 1. Crosshair của Shift Lock có độ ưu tiên cao nhất. Nếu Shift Lock bật, nó sẽ luôn hiển thị.
        _uiManager.SetShiftLockCrosshair(isShiftLock);

        // 2. Crosshair của góc nhìn thứ nhất chỉ hiển thị khi:
        //    - Đang ở góc nhìn thứ nhất (isFirstPerson = true)
        //    - VÀ Shift Lock KHÔNG bật (isShiftLock = false)
        _uiManager.SetFirstPersonCrosshair(isFirstPerson && !isShiftLock);
    }

    /// <summary>
    /// Hàm trung tâm để quản lý trạng thái của con trỏ.
    /// Quyết định xem con trỏ nên bị khóa hay không dựa trên các trạng thái của game.
    /// </summary>
    private void UpdateCursorState()
    {
        if (Mouse.current == null) return;

        // Con trỏ sẽ bị khóa nếu: ở góc nhìn thứ nhất, HOẶC bật shift lock, HOẶC đang giữ chuột phải.
        bool shouldBeLocked = isFirstPerson || isShiftLock || Mouse.current.rightButton.isPressed;

        if (shouldBeLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleRotation()
    {
        // Hàm này xử lý việc xoay camera và trạng thái con trỏ khi giữ RMB hoặc ở FPS.
        bool shouldRotateCamera = false;
        
        // Camera sẽ xoay khi ở chế độ First Person hoặc Shift Lock.
        if (isFirstPerson || isShiftLock)
        {
            shouldRotateCamera = true;
        }
        // Hoặc khi giữ chuột phải ở góc nhìn thứ ba.
        else
        {
            if (Mouse.current.rightButton.isPressed)
            {
                shouldRotateCamera = true;
            }
        }

        // --- Thực hiện xoay camera nếu cần ---
        if (shouldRotateCamera)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            float yInput = invertY ? -mouseDelta.y : mouseDelta.y;
            
            // Logic xoay camera được áp dụng chung cho cả hai góc nhìn
            RotateCameras(mouseDelta.x, yInput);
        }
    }

    /// <summary>
    /// Xoay nhân vật theo hướng camera khi ở chế độ First Person hoặc Shift Lock.
    /// Hàm này được gọi trong LateUpdate để đảm bảo nó chạy sau khi camera đã được cập nhật.
    /// </summary>
    private void HandleCharacterRotationWithCamera()
    {
        // Không xoay nhân vật nếu đang trong trạng thái ragdoll.
        if ((_ragdollController != null && _ragdollController.IsRagdollActive) 
            || _playerController == null 
            || Camera.main == null) return;

        // Chỉ xoay nhân vật khi ở chế độ First Person hoặc Shift Lock.
        if (isFirstPerson || isShiftLock)
        {
            _playerTransform.rotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
        }
    }

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y / 120f;
        if (scroll == 0) return;

        if (isFirstPerson)
        {
            if (scroll < 0)
            {
                // Lăn chuột lùi khi đang ở First Person -> Thoát ra Third Person
                if (orbitalFollow != null) orbitalFollow.Radius = minZoom;
                if (thirdPersonFollow != null) thirdPersonFollow.CameraDistance = minZoom;
                SetFirstPersonMode(false);
            }
            return; // Khóa không cho radius thay đổi khi đang ở First Person
        }

        if (thirdPersonFollow != null)
        {
            thirdPersonFollow.CameraDistance -= scroll * zoomSpeed;
            if (enableFirstPerson && thirdPersonFollow.CameraDistance < firstPersonThreshold)
            {
                SetFirstPersonMode(true);
            }
            else
            {
                thirdPersonFollow.CameraDistance = Mathf.Clamp(thirdPersonFollow.CameraDistance, minZoom, maxZoom);
            }
        }
        else if (orbitalFollow != null)
        {
            orbitalFollow.Radius -= scroll * zoomSpeed;
            if (enableFirstPerson && orbitalFollow.Radius < firstPersonThreshold)
            {
                SetFirstPersonMode(true);
            }
            else
            {
                orbitalFollow.Radius = Mathf.Clamp(orbitalFollow.Radius, minZoom, maxZoom);
            }
        }
    }

    /// <summary>
    /// Xoay camera phù hợp dựa trên trạng thái hiện tại (First/Third Person).
    /// </summary>
    private void RotateCameras(float xInput, float yInput)
    {
        if (isFirstPerson)
        {
            // Xoay First Person Camera
            if (firstPersonPanTilt != null)
            {
                firstPersonPanTilt.PanAxis.Value += xInput * lookSpeed;
                firstPersonPanTilt.TiltAxis.Value += yInput * lookSpeed * 0.7f;
                
                // Giới hạn trục Y (Tilt thường giới hạn từ -90 đến 90)
                firstPersonPanTilt.TiltAxis.Value = Mathf.Clamp(firstPersonPanTilt.TiltAxis.Value, -90f, 90f);
            }
        }
        else
        {
            // Xoay Third Person Camera (bao gồm cả khi đang bật Shift Lock hoặc giữ RMB)
            if (orbitalFollow != null)
            {
                orbitalFollow.HorizontalAxis.Value += xInput * lookSpeed;
                orbitalFollow.VerticalAxis.Value += yInput * lookSpeed * 0.7f;
                
                // Giới hạn trục Y
                orbitalFollow.VerticalAxis.Value = Mathf.Clamp(orbitalFollow.VerticalAxis.Value, -89f, 89f);
            }
        }
    }

    private void SetFirstPersonMode(bool state)
    {
        isFirstPerson = state;
        
        UpdateCrosshairs();

        // Đồng bộ góc quay giữa 2 camera để không bị giật hướng nhìn
        if (orbitalFollow != null && firstPersonPanTilt != null)
        {
            if (state) // Từ 3rd sang 1st
            {
                firstPersonPanTilt.PanAxis.Value = orbitalFollow.HorizontalAxis.Value;
                firstPersonPanTilt.TiltAxis.Value = orbitalFollow.VerticalAxis.Value;
            }
            else // Từ 1st về 3rd
            {
                orbitalFollow.HorizontalAxis.Value = firstPersonPanTilt.PanAxis.Value;
                orbitalFollow.VerticalAxis.Value = firstPersonPanTilt.TiltAxis.Value;
            }
        }

        // Chuyển đổi Priority của Camera
        if (thirdPersonCamera != null) thirdPersonCamera.Priority = state ? 0 : 10;
        if (firstPersonCamera != null) firstPersonCamera.Priority = state ? 10 : 0;

        // Khi thoát khỏi chế độ người thứ nhất, cần cập nhật lại mục tiêu camera.
        // Trạng thái con trỏ sẽ được xử lý bởi UpdateCursorState().
        if (!state)
        {
            UpdateCameraTargets();
        }

        // Ẩn/Hiện nhân vật
        if (playerRenderers != null)
        {
            foreach (var rend in playerRenderers)
            {
                if (rend != null) rend.enabled = !state;
            }
        }
    }
}
}
