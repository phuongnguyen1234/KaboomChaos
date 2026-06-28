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

        // Trả về vị trí đỉnh giữa (top-center) của BoxCollider trong không gian thế giới
        public Vector3 SpawnPoint
        {
            get
            {
                if (_boxCollider == null)
                {
                    Debug.LogError("PlayerSpawn requires a BoxCollider!", this);
                    return transform.position;
                }
                
                // Chuyển đổi tâm và kích thước collider sang không gian thế giới
                Vector3 worldCenter = transform.TransformPoint(_boxCollider.center);
                Vector3 worldSize = Vector3.Scale(_boxCollider.size, transform.lossyScale); // Tính cả scale của GameObject
                
                // Cộng thêm nửa chiều cao theo hướng "up" của đối tượng để lấy đỉnh giữa
                return worldCenter + transform.up * (worldSize.y / 2f);
            }
        }

        // Hiển thị Gizmos trong Editor để dễ dàng nhìn thấy điểm spawn và collider
        private void OnDrawGizmos()
        {
            if (_boxCollider == null)
            {
                _boxCollider = GetComponent<BoxCollider>();
            }

            if (_boxCollider != null)
            {
                // Vẽ khung dây của BoxCollider
                Gizmos.color = Color.green;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(_boxCollider.center, _boxCollider.size);
                Gizmos.matrix = Matrix4x4.identity;

                // Vẽ điểm SpawnPoint thực tế
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(SpawnPoint, 0.2f);
            }
            else
            {
                // Cảnh báo nếu không có BoxCollider (mặc dù RequireComponent sẽ tự thêm)
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
                _boxCollider.isTrigger = true; // Thường thì điểm spawn nên là trigger
            }
            // Đặt kích thước mặc định nếu collider có kích thước bằng 0
            if (_boxCollider.size == Vector3.zero)
            {
                _boxCollider.size = new Vector3(1, 1, 1);
            }
        }
    }
}