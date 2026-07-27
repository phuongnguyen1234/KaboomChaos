using UnityEngine;
using TMPro;
using DG.Tweening;
using Core; // Namespace chứa GameEvents của bạn
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Điều khiển animation và hành vi của một đối tượng text nổi (ví dụ: số sát thương).
    /// Yêu cầu đối tượng phải có component TextMeshPro.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class FloatingTextController : MonoBehaviour, IFloatingTextController
    {
        [Header("Animation Settings")]
        [Tooltip("Quãng đường text sẽ di chuyển lên trên.")]
        [SerializeField] private float _moveUpDistance = 1.5f;
        [Tooltip("Tổng thời gian diễn ra animation.")]
        [SerializeField] private float _duration = 1.2f;
        [Tooltip("Kiểu easing cho chuyển động đi lên.")]
        [SerializeField] private Ease _moveEase = Ease.OutQuad;
        [Tooltip("Kiểu easing cho hiệu ứng mờ dần.")]
        [SerializeField] private Ease _fadeEase = Ease.InQuad;

        private TextMeshPro _textMesh;
        private Camera _mainCamera;
        private Sequence _activeSequence;

        // IFloatingTextController implementation
        public GameObject GameObject => gameObject;

        private void Awake()
        {
            _textMesh = GetComponent<TextMeshPro>();
            _mainCamera = Camera.main;
        }

        private void OnDisable()
        {
            // Luôn hủy sequence đang chạy khi object bị disable (trả về pool).
            // Điều này ngăn chặn một animation cũ hoàn thành trên một object đã được tái sử dụng.
            _activeSequence?.Kill();
        }

        private void LateUpdate()
        {
            // Luôn xoay text hướng về phía camera để đảm bảo nó luôn dễ đọc.
            if (_mainCamera != null)
            {
                transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                                 _mainCamera.transform.rotation * Vector3.up);
            }
        }

        /// <summary>
        /// Kích hoạt animation của text nổi. Animation này di chuyển text cục bộ (local space),
        /// cho phép nó đi theo đối tượng cha nếu có.
        /// </summary>
        /// <param name="text">Nội dung để hiển thị.</param>
        /// <param name="color">Màu sắc của text.</param>
        public void Trigger(string text, Color color)
        {
            _activeSequence?.Kill(); // Dừng animation cũ nếu có

            _textMesh.text = text;
            _textMesh.color = color;
            _textMesh.alpha = 1f; // Reset độ trong suốt

            // Animation sẽ dựa trên localPosition để di chuyển cùng với parent.
            Vector3 startPosition = transform.localPosition;
            Vector3 endPosition = startPosition + (Vector3.up * _moveUpDistance);

            _activeSequence = DOTween.Sequence();

            // Thêm animation di chuyển và mờ dần vào sequence
            // Sửa lỗi: TextMeshPro không có DOFade. Dùng DOTween.ToAlpha để tween màu của text.
            // Cách này an toàn và không yêu cầu module DOTween TextMeshPro phải được cài đặt.
            _activeSequence.Append(transform.DOLocalMove(endPosition, _duration).SetEase(_moveEase));
            _activeSequence.Join(DOTween.ToAlpha(() => _textMesh.color, x => _textMesh.color = x, 0f, _duration).SetEase(_fadeEase));

            // Khi animation hoàn tất, yêu cầu được trả về pool thông qua hệ thống event.
            _activeSequence.OnComplete(() =>
            {
                // Giả định rằng bạn có một hệ thống GameEvents tương tự như trong các script khác.
                GameEvents.TriggerFloatingTextDespawnRequest(gameObject);
            });
        }
    }
}
