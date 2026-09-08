using UnityEngine;
using System.Collections;

namespace Core
{
    /// <summary>
    /// Quản lý vòng đời của một GameObject, khiến nó tự hủy (trả về pool) sau một khoảng thời gian..
    /// Có thể được sử dụng cho các hiệu ứng tạm thời như khí độc, khói, hoặc các mảnh vỡ tạm thời..
    /// </summary>
    public class LifetimeController : MonoBehaviour
    {
        public enum DespawnActionType
        {
            [Tooltip("Trả đối tượng về VFX Pool (dùng cho hiệu ứng hình ảnh).")]
            VFX,
            [Tooltip("Trả đối tượng về Block Pool (dùng cho các khối địa hình.)")]
            Block,
            [Tooltip("Hủy đối tượng ngay lập tức (chỉ dùng khi không có pool).")]
            Destroy
        }

        [Header("Lifetime Settings")]
        [Tooltip("Thời gian (giây) đối tượng này sẽ tồn tại trước khi tự hủy.")]
        [SerializeField] private float _lifetime =  10f;

        [Tooltip("Loại hành động sẽ được thực hiện khi hết thời gian.")]
        [SerializeField] private DespawnActionType _despawnAction = DespawnActionType.VFX;



        [Header("Fade Out Settings")]
        [Tooltip("Bật tính năng mờ dần (fade out) mesh trước khi hết thời gian tồn tại.")]
        [SerializeField] private bool _fadeOutBeforeDespawn = false;
        [Tooltip("Thời gian (giây) mờ dần mesh trước khi hết thời gian tồn tại.")]
        [SerializeField] private float _fadeOutDuration =  0.5f;
        [Tooltip("MeshRenderer cụ thể sẽ được fade khi hết thời gian. Bỏ trống sẽ tự tìm MaterialEffectController trong các object con.")]
        [SerializeField] private MeshRenderer _meshRenderer;

        private MaterialEffectController _materialEffectController;
        private Coroutine _lifetimeCoroutine;

        private void OnEnable()
        {
            // Cache component MaterialEffectController de doi alpha khi fade..
            // Neu da gan MeshRenderer rieng, uu tien tim MaterialEffectController tren chinh mesh do
            // de fade dung vat the (mesh) nguoi dung mong muon, thay vi fade bat ky mesh con nao dau tien.
            if (_materialEffectController == null)
            {
                if (_meshRenderer != null)
                {
                    _materialEffectController = _meshRenderer.GetComponent<MaterialEffectController>();
                }
                if (_materialEffectController == null)
                {
                    _materialEffectController = GetComponentInChildren<MaterialEffectController>();
                }
            }

            // Khi đối tượng được kích hoạt(ví dụ: khi được lấy ra từ pool), bắt đầu đếm ngược vòng đời..
            // Dừng coroutine cũ(nếu có) để đảm bảo không có bộ đếm nào chạy chồng chéo từ "kiếp trước".,
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
            }
            _lifetimeCoroutine = StartCoroutine(LifetimeRoutine());;


            // Đăng ký lắng nghe sự kiện dọn dẹp cuối round..

            GameEvents.OnRoundEndCleanup += Despawn;

        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh memory leak khi object được trả về pool..
            GameEvents.OnRoundEndCleanup -= Despawn;

        }

        private IEnumerator LifetimeRoutine()
        {
            // Neu bat tinh nang fade, mesh se mo dan truoc khi despawn trong khoang thoi gian cuoi..
            float fadeDuration = _fadeOutBeforeDespawn ? Mathf.Clamp(_fadeOutDuration,  0f, _lifetime) :  0f;
            yield return new WaitForSeconds(Mathf.Max(0f, _lifetime - fadeDuration));
            if (fadeDuration >  0f) yield return FadeOutMesh(fadeDuration);
            Despawn();
        }

        /// <summary>
        /// Thực hiện hành động despawn..
        /// Được gọi bởi coroutine hết hạn hoặc bởi sự kiện dọn dẹp cuối round..
        /// </summary>
        private void Despawn()
        {
            // Dừng coroutine để tránh gọi despawn hai lần..
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

        /// <summary>
        /// Mo dan(fade out) alpha cua mesh ve 0 de tao hieu ung bien mat truoc khi despawn..
        /// </summary>
        private IEnumerator FadeOutMesh(float fadeDuration)
        {
            if (_materialEffectController == null || fadeDuration <=  0f) yield break;

            float startAlpha = _materialEffectController.CustomAlpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                _materialEffectController.CustomAlpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
            _materialEffectController.CustomAlpha =  0f;
        }
    }
}
