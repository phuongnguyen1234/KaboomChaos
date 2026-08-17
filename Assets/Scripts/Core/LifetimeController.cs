using UnityEngine;
using System.Collections;

namespace Core
{
    /// <summary>
    /// Quản lý vòng đời của một GameObject, khiến nó tự hủy (trả về pool) sau một khoảng thời gian.
    /// Có thể được sử dụng cho các hiệu ứng tạm thời như khí độc, khói, hoặc các mảnh vỡ tạm thời.
    /// </summary>
    public class LifetimeController : MonoBehaviour
    {
        public enum DespawnActionType
        {
            [Tooltip("Trả đối tượng về VFX Pool (dùng cho hiệu ứng hình ảnh).")]
            VFX,
            [Tooltip("Trả đối tượng về Block Pool (dùng cho các khối địa hình).")]
            Block,
            [Tooltip("Hủy đối tượng ngay lập tức (chỉ dùng khi không có pool).")]
            Destroy
        }

        [Header("Lifetime Settings")]
        [Tooltip("Thời gian (giây) đối tượng này sẽ tồn tại trước khi tự hủy.")]
        [SerializeField] private float _lifetime = 10f;

        [Tooltip("Loại hành động sẽ được thực hiện khi hết thời gian.")]
        [SerializeField] private DespawnActionType _despawnAction = DespawnActionType.VFX;
        private Coroutine _lifetimeCoroutine;

        private void OnEnable()
        {
            // Khi đối tượng được kích hoạt (ví dụ: khi được lấy ra từ pool), bắt đầu đếm ngược vòng đời.
            // Dừng coroutine cũ (nếu có) để đảm bảo không có bộ đếm nào chạy chồng chéo từ "kiếp trước".
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
            }
            _lifetimeCoroutine = StartCoroutine(LifetimeRoutine());

            // Đăng ký lắng nghe sự kiện dọn dẹp cuối round.
            GameEvents.OnRoundEndCleanup += Despawn;
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh memory leak khi object được trả về pool.
            GameEvents.OnRoundEndCleanup -= Despawn;
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(_lifetime);
            Despawn();
        }

        /// <summary>
        /// Thực hiện hành động despawn.
        /// Được gọi bởi coroutine hết hạn hoặc bởi sự kiện dọn dẹp cuối round.
        /// </summary>
        private void Despawn()
        {
            // Dừng coroutine để tránh gọi despawn hai lần.
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
                _lifetimeCoroutine = null;
            }

            if (!gameObject.activeInHierarchy) return; // Tránh lỗi nếu đã bị despawn

            switch (_despawnAction)
            {
                case DespawnActionType.VFX:
                    GameEvents.TriggerVFXDespawnRequest(gameObject);
                    break;
                case DespawnActionType.Block:
                    GameEvents.TriggerBlockDespawnRequest(gameObject);
                    break;
                case DespawnActionType.Destroy:
                    Destroy(gameObject);
                    break;
            }
        }
    }
}