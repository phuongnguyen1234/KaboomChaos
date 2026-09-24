using UnityEngine;
using TMPro;
using DG.Tweening;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Điều khiển animation và hành vi của một đối tượng text nổi trên hệ thống UI Canvas.
    /// Hoạt động độc lập hoàn toàn trên UI sau khi được khởi tạo.
    /// </summary>    
    public class FloatingTextController : MonoBehaviour, IFloatingTextController
    {
        [Header("Animation Settings")]
        [Tooltip("Quãng đường text sẽ di chuyển lên trên (theo pixel UI).")]
        [SerializeField] private float _moveUpDistance = 100f;
        [Tooltip("Tổng thời gian diễn ra animation.")]
        [SerializeField] private float _duration = 1.2f;
        [Tooltip("Kiểu easing cho chuyển động đi lên.")]
        [SerializeField] private Ease _moveEase = Ease.OutQuad;
        [Tooltip("Kiểu easing cho hiệu ứng mờ dần.")]
        [SerializeField] private Ease _fadeEase = Ease.InQuad;

        [Header("Icon Settings")]
        [Tooltip("GameObject chứa icon sẽ hiển thị bên cạnh text.")]
        [SerializeField] private GameObject _iconGameObject;

        private TextMeshProUGUI _textMesh;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Sequence _activeSequence;

        public GameObject GameObject => gameObject;

        private void Awake()
        {
            _textMesh = GetComponentInChildren<TextMeshProUGUI>(true);
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_textMesh == null)
            {
                Debug.LogError("[FloatingTextController] Không tìm thấy TextMeshProUGUI trong các đối tượng con. Hãy chắc chắn prefab đã chuyển sang UI.", this);
            }
        }

        private void OnDisable()
        {
            _activeSequence?.Kill();
            SetIconVisibility(false);
        }

        public void Trigger(string text, Color color, bool showIcon)
        {
            if (_textMesh == null) return;
            
            _activeSequence?.Kill();

            _textMesh.text = text;
            _textMesh.color = color;
            _textMesh.alpha = 1f;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
            
            SetIconVisibility(showIcon);

            _activeSequence = DOTween.Sequence();

            // Lấy vị trí anchored hiện tại và cộng thêm khoảng cách bay lên
            float targetY = _rectTransform.anchoredPosition.y + _moveUpDistance;

            // Di chuyển thẳng trên RectTransform
            _activeSequence.Append(_rectTransform.DOAnchorPosY(targetY, _duration).SetEase(_moveEase));
            
            // Lam mo dan toan bo CanvasGroup (bao gom ca text va icon)
            if (_canvasGroup != null)
            {
                _activeSequence.Join(_canvasGroup.DOFade(0f, _duration).SetEase(_fadeEase));
            }
            else
            {
                _activeSequence.Join(DOTween.ToAlpha(() => _textMesh.color, x => _textMesh.color = x, 0f, _duration).SetEase(_fadeEase));
            }

            _activeSequence.OnComplete(() =>
            {
                GameEvents.TriggerFloatingTextDespawnRequest(gameObject);
            });
        }

        private void SetIconVisibility(bool visible)
        {
            if (_iconGameObject != null && _iconGameObject.activeSelf != visible) 
                _iconGameObject.SetActive(visible);
        }
    }
}
