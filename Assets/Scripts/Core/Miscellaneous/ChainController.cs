using UnityEngine;

namespace Core.Miscellaneous
{
    /// <summary>
    /// Điều khiển việc hiển thị một đường thẳng (dây xích) giữa hai điểm.
    /// Thường được sử dụng cho Thủy lôi (Naval Mine).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ChainController : MonoBehaviour
    {
        [Header("Chain Appearance")]
        [Tooltip("Số đoạn để tạo thành dây xích. Càng nhiều đoạn, xích càng mượt.")]
        [SerializeField] private int _segments = 20;

        [Tooltip("Độ võng tối đa của dây xích. Giá trị lớn hơn làm xích chùng xuống nhiều hơn.")]
        [SerializeField] private float _maxSag = 1.5f;

        private LineRenderer _lineRenderer;
        private Transform _startPoint;
        private Transform _endPoint;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.enabled = false; // Bắt đầu ở trạng thái bị vô hiệu hóa
        }

        public void SetEndpoints(Transform start, Transform end)
        {
            _startPoint = start;
            _endPoint = end;
            if (_startPoint != null && _endPoint != null)
            {
                _lineRenderer.enabled = true;
                UpdateChain();
            }
        }

        private void LateUpdate()
        {
            // Cập nhật nếu các điểm cuối hợp lệ, nếu không thì tự hủy
            if (_startPoint != null && _endPoint != null && _endPoint.gameObject.activeInHierarchy)
            {
                UpdateChain();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void UpdateChain()
        {
            if (!_lineRenderer.enabled || _startPoint == null || _endPoint == null) return;

            // Đảm bảo số đoạn luôn lớn hơn 0
            int segmentCount = Mathf.Max(1, _segments);
            _lineRenderer.positionCount = segmentCount + 1;

            Vector3 startPos = _startPoint.position;
            Vector3 endPos = _endPoint.position;

            // Tính toán độ võng hiệu quả dựa trên khoảng cách, để dây xích căng ra khi ở xa
            float distance = Vector3.Distance(startPos, endPos);
            // Độ võng sẽ tăng theo khoảng cách nhưng được giới hạn bởi _maxSag
            // và gần như bằng 0 khi các điểm rất gần nhau.
            float effectiveSag = Mathf.Lerp(0, _maxSag, Mathf.Clamp01(distance / (_maxSag * 4f)));

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                // Vị trí trên đường thẳng
                Vector3 position = Vector3.Lerp(startPos, endPos, t);

                // Thêm độ võng (parabolic curve) hướng xuống dưới
                float curve = 4 * (t - t * t); // Parabol có giá trị từ 0 -> 1 -> 0
                position += Vector3.down * curve * effectiveSag;

                _lineRenderer.SetPosition(i, position);
            }
        }

        /// <summary>
        /// Tách dây xích và phá hủy nó.
        /// </summary>
        public void Detach()
        {
            _startPoint = null;
            _endPoint = null;
            Destroy(gameObject);
        }
    }
}