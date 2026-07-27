using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(MeshCollider))] // Đảm bảo đối tượng luôn có MeshCollider
    public class PlayerSpawn : MonoBehaviour
    {
        private MeshCollider _meshCollider;

        private void Awake()
        {
            _meshCollider = GetComponent<MeshCollider>();
        }

        // Trả về vị trí đỉnh giữa (top-center) của AABB của MeshCollider trong không gian thế giới
        public Vector3 SpawnPoint
        {
            get
            {
                if (_meshCollider == null)
                {
                    Debug.LogError("PlayerSpawn requires a MeshCollider!", this);
                    return transform.position;
                }

                // Bounds của MeshCollider đã ở trong không gian thế giới (world space).
                // Lấy điểm trung tâm trên cùng của bounding box để làm điểm spawn.
                return _meshCollider.bounds.center + Vector3.up * _meshCollider.bounds.extents.y;
            }
        }

        // Hiển thị Gizmos trong Editor để dễ dàng nhìn thấy điểm spawn và collider
        private void OnDrawGizmos()
        {
            // Lấy component trong OnDrawGizmos để thay đổi được thấy ngay trong Editor
            if (_meshCollider == null)
            {
                _meshCollider = GetComponent<MeshCollider>();
            }

            if (_meshCollider != null && _meshCollider.sharedMesh != null)
            {
                // Vẽ khung dây của Bounding Box (AABB) của MeshCollider
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f); // Màu xanh lá, hơi trong suốt
                Gizmos.DrawWireCube(_meshCollider.bounds.center, _meshCollider.bounds.size);

                // Vẽ điểm SpawnPoint thực tế
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(SpawnPoint, 0.2f);
            }
            else
            {
                // Cảnh báo nếu không có MeshCollider hoặc mesh chưa được gán
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, Vector3.one);
            }
        }

        // Phương thức Reset được gọi khi thêm component lần đầu hoặc click Reset trong Inspector
        private void Reset()
        {
            _meshCollider = GetComponent<MeshCollider>();
            if (_meshCollider == null)
            {
                _meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            // Một MeshCollider để làm trigger thì bắt buộc phải là 'convex'.
            _meshCollider.convex = true;
            _meshCollider.isTrigger = true; // Thường thì điểm spawn nên là trigger
        }
    }
}