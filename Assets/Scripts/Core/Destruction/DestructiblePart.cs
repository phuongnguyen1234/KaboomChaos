using UnityEngine;

namespace Core
{
    /// <summary>
    /// Đại diện cho một mảnh vỡ trong một công trình có thể bị phá hủy.
    /// Quản lý trạng thái vật lý (kinematic/dynamic) và các kết nối của nó.
    /// </summary>
    [RequireComponent(typeof(Collider))] // Đảm bảo mỗi mảnh vỡ luôn có một Collider
    [RequireComponent(typeof(Rigidbody))]
    public class DestructiblePart : MonoBehaviour
    {
        #region Fields
        [Header("State")]
        [SerializeField] private PartState _currentState = PartState.Intact; // Mặc định là Intact

        [Header("Destruction")]
        [Tooltip("Số lần va chạm cần thiết để phá hủy mảnh này.")]
        [SerializeField] private int _initialMaxHits = 1;

        private Rigidbody _rigidbody;
        private int _hitCount = 0;
        #endregion

        #region Properties
        public int MaxHits { get; set; }
        public PartState CurrentState => _currentState;
        public GameObject GameObject => gameObject;
        public int HitCount => _hitCount; // Số lần bị bắn trúng
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            MaxHits = _initialMaxHits;
            _rigidbody.isKinematic = true; // Mặc định là tĩnh
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Đăng ký một lần bị bắn trúng.
        /// </summary>
        public void RegisterHit()
        {
            _hitCount++;
        }

        /// <summary>
        /// Kích hoạt vật lý và áp dụng lực nổ.
        /// </summary>
        public void ApplyExplosionForce(Vector3 explosionPosition, float explosionForce, float explosionRadius)
        {
            if (_currentState == PartState.Intact)
            {
                _currentState = PartState.Loose;
                _rigidbody.isKinematic = false;
            }
            
            // Sử dụng ForceMode.Impulse để tạo ra một cú đẩy tức thời, phù hợp với các vụ nổ.
            // Nó sẽ tạo ra một thay đổi vận tốc ngay lập tức, không phụ thuộc vào thời gian của một frame.
            _rigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, 0.0f, ForceMode.Impulse);
        }
        #endregion

        #if UNITY_EDITOR
        #region Gizmos
        /// <summary>
        /// Vẽ Gizmos khi đối tượng được chọn trong Editor.
        /// Hàm này vẽ một khung dây màu xanh dương để làm nổi bật mảnh đang được chọn.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Vẽ một khối màu xanh dương để làm nổi bật mảnh đang được chọn.
            Gizmos.color = Color.cyan;
            if (TryGetComponent<Collider>(out var selectedCollider))
            {
                Gizmos.DrawWireCube(selectedCollider.bounds.center, selectedCollider.bounds.size);
            }
        }
        #endregion
#endif
    }
}