using UnityEngine;

namespace Core.Utilities
{
    /// <summary>
    /// Script hỗ trợ xoay đối tượng quanh các trục được chỉ định theo thời gian.
    /// Hỗ trợ xoay qua Transform hoặc Rigidbody/Rigidbody2D.
    /// </summary>
    public class ObjectRotator : MonoBehaviour
    {
        #region Fields
        [Tooltip("Tốc độ xoay trên từng trục (độ/giây).")]
        [SerializeField] private Vector3 _rotationSpeed = new Vector3(0f, 90f, 0f);

        [Tooltip("Không gian xoay (World hoặc Self/Local).")]
        [SerializeField] private Space _rotationSpace = Space.World;

        [Tooltip("Sử dụng Rigidbody/Rigidbody2D để xoay thay vì Transform (cần cho object vật lý).")]
        [SerializeField] private bool _usePhysics = false;

        private Rigidbody _rb;
        private Rigidbody2D _rb2d;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_usePhysics)
            {
                _rb = GetComponent<Rigidbody>();
                _rb2d = GetComponent<Rigidbody2D>();

                if (_rb == null && _rb2d == null)
                {
                    Debug.LogWarning("ObjectRotator được thiết lập dùng Physics nhưng không tìm thấy Rigidbody/Rigidbody2D. Sẽ tự động dùng Transform.", this);
                    _usePhysics = false;
                }
            }
        }

        private void Update()
        {
            if (!_usePhysics && _rotationSpeed != Vector3.zero)
            {
                transform.Rotate(_rotationSpeed * Time.deltaTime, _rotationSpace);
            }
        }

        private void FixedUpdate()
        {
            if (_usePhysics && _rotationSpeed != Vector3.zero)
            {
                if (_rb != null)
                {
                    Quaternion deltaRotation = Quaternion.Euler(_rotationSpeed * Time.fixedDeltaTime);
                    Quaternion targetRotation = _rotationSpace == Space.World 
                        ? deltaRotation * _rb.rotation 
                        : _rb.rotation * deltaRotation;
                        
                    _rb.MoveRotation(targetRotation);
                }
                else if (_rb2d != null)
                {
                    _rb2d.MoveRotation(_rb2d.rotation + _rotationSpeed.z * Time.fixedDeltaTime);
                }
            }
        }
        #endregion
    }
}

