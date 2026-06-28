using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("Kéo Camera Góc nhìn thứ 3 (Orbital Follow) vào đây. Nếu bỏ trống, script sẽ tự tìm theo tên 'ThirdPersonCamera'")]
    public CinemachineCamera thirdPersonCamera;
    [Tooltip("Kéo Camera Góc nhìn thứ nhất (Pan Tilt) vào đây. Nếu bỏ trống, script sẽ tự tìm theo tên 'FirstPersonCamera'")]
    public CinemachineCamera firstPersonCamera;

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
    [Tooltip("Camera dịch sang phải Player bao nhiêu khi bật Shift Lock (theo local right của Player)")]
    public float shiftLockSideOffset = 0.75f;
    private bool isShiftLock = false;
    private Transform _shiftLockFollowTarget; // Điểm Follow offset sang bên được tạo động
    private Transform _defaultFollowTarget;   // Điểm Follow mặc định (chính Player)

    [Header("Input Settings")]
    [Tooltip("Kéo Action tương ứng với nút Shift Lock vào đây")]
    public InputActionReference shiftLockAction;

    private Vector2 savedMousePos;
    
    // Components
    private CinemachineThirdPersonFollow thirdPersonFollow;
    private CinemachineOrbitalFollow orbitalFollow;
    private CinemachinePanTilt firstPersonPanTilt;

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
        if (thirdPersonCamera != null) thirdPersonCamera.Follow = this.transform;
        if (firstPersonCamera != null) firstPersonCamera.Follow = this.transform; // Component HardLockToTarget cũng sẽ tự dùng target này

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

        // Tạo động điểm Follow lệch sang bên (dùng cho Shift Lock)
        var offsetObj = new GameObject("[ShiftLockFollowPoint]");
        offsetObj.transform.SetParent(this.transform);
        offsetObj.transform.localPosition = new Vector3(shiftLockSideOffset, 0f, 0f);
        _shiftLockFollowTarget = offsetObj.transform;
        _defaultFollowTarget = this.transform;

        // Cài đặt ưu tiên ban đầu
        SetFirstPersonMode(false);
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // Bật/tắt Shift Lock (chỉ khi không ở First Person)
        if (enableShiftLock && !isFirstPerson && shiftLockAction != null && shiftLockAction.action.WasPressedThisFrame())
        {
            isShiftLock = !isShiftLock;
            
            // Khóa/Mở khóa con trỏ ngay khi bật/tắt Shift Lock
            if (isShiftLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Chuyển điểm Follow ngay khi toggle
            ApplyShiftLockFollowTarget();
        }

        HandleRotation();
        HandleZoom();
    }


    /// <summary>
    /// Chuyển điểm Follow của ThirdPersonCamera sang offset khi bật Shift Lock.
    /// </summary>
    private void ApplyShiftLockFollowTarget()
    {
        if (thirdPersonCamera == null) return;

        thirdPersonCamera.Follow = isShiftLock ? _shiftLockFollowTarget : _defaultFollowTarget;
    }

    private void HandleRotation()
    {
        bool shouldRotateCamera = false;

        if (isFirstPerson || isShiftLock)
        {
            // Góc nhìn thứ nhất HOẶC Shift Lock: luôn khóa con trỏ ở giữa và xoay camera
            shouldRotateCamera = true;
        }
        else
        {
            // Góc nhìn thứ ba thông thường: khóa con trỏ khi giữ chuột phải
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                savedMousePos = Mouse.current.position.ReadValue();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Mouse.current.WarpCursorPosition(savedMousePos);
                Cursor.visible = true;
            }

            if (Mouse.current.rightButton.isPressed)
            {
                shouldRotateCamera = true;
            }
        }

        if (shouldRotateCamera)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            float yInput = invertY ? -mouseDelta.y : mouseDelta.y;
            
            if (isFirstPerson)
            {
                // Xoay First Person Camera
                if (firstPersonPanTilt != null)
                {
                    firstPersonPanTilt.PanAxis.Value += mouseDelta.x * lookSpeed;
                    firstPersonPanTilt.TiltAxis.Value += yInput * lookSpeed * 0.7f;
                    
                    // Giới hạn trục Y (Tilt thường giới hạn từ -90 đến 90)
                    firstPersonPanTilt.TiltAxis.Value = Mathf.Clamp(firstPersonPanTilt.TiltAxis.Value, -90f, 90f);
                }
            }
            else
            {
                // Xoay Third Person Camera (bao gồm cả khi đang bật Shift Lock)
                if (orbitalFollow != null)
                {
                    orbitalFollow.HorizontalAxis.Value += mouseDelta.x * lookSpeed;
                    orbitalFollow.VerticalAxis.Value += yInput * lookSpeed * 0.7f;
                    
                    // Giới hạn trục Y
                    orbitalFollow.VerticalAxis.Value = Mathf.Clamp(orbitalFollow.VerticalAxis.Value, -89f, 89f);
                }
            }
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

    private void SetFirstPersonMode(bool state)
    {
        isFirstPerson = state;

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

        // Cập nhật trạng thái con trỏ
        if (state)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true; // Nên dùng ảnh UI thay vì con trỏ hệ thống
        }
        else
        {
            ApplyShiftLockFollowTarget();
            
            // Khi thoát First Person, kiểm tra xem có đang giữ chuột không
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (!isShiftLock)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
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
