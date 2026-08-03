using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(BoxCollider))] // Đảm bảo đối tượng luôn có BoxCollider
    public class PlayerSpawn : MonoBehaviour
    {
        private BoxCollider _boxCollider;

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider>();
        }

        // Trả về vị trí đỉnh giữa (top-center) của AABB của BoxCollider trong không gian thế giới
        public Vector3 SpawnPoint
        {
            get
            {
                if (_boxCollider == null)
                {
                    Debug.LogError("PlayerSpawn requires a BoxCollider!", this);
                    return transform.position;
                }

                // Bounds của BoxCollider đã ở trong không gian thế giới (world space).
                // Lấy điểm trung tâm trên cùng của bounding box để làm điểm spawn.
                return _boxCollider.bounds.center + Vector3.up * _boxCollider.bounds.extents.y;
            }
        }

        // Hiển thị Gizmos trong Editor để dễ dàng nhìn thấy điểm spawn và collider
        private void OnDrawGizmos()
        {
            // Lấy component trong OnDrawGizmos để thay đổi được thấy ngay trong Editor
            if (_boxCollider == null)
            {
                _boxCollider = GetComponent<BoxCollider>();
            }

            if (_boxCollider != null)
            {
                // Vẽ khung dây của Bounding Box (AABB) của BoxCollider
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f); // Màu xanh lá, hơi trong suốt
                Gizmos.DrawWireCube(_boxCollider.bounds.center, _boxCollider.bounds.size);

                // Vẽ điểm SpawnPoint thực tế
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(SpawnPoint, 0.2f);
            }
            else
            {
                // Cảnh báo nếu không có BoxCollider
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, Vector3.one);
            }
        }

        // Phương thức Reset được gọi khi thêm component lần đầu hoặc click Reset trong Inspector
        private void Reset()
        {
            _boxCollider = GetComponent<BoxCollider>();
            if (_boxCollider == null)
            {
                _boxCollider = gameObject.AddComponent<BoxCollider>();
            }

            _boxCollider.isTrigger = true; // Thường thì điểm spawn nên là trigger
        }
    }
}