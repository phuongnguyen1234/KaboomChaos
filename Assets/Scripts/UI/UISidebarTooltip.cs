using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Component hieu ung tooltip cho cac nut tren sidebar.
    /// An va luu vi tri ban dau luc Awake, sau do chay animation hover:
    /// slide in tu trai sang phai + fade in khi hover vao,
    /// fade out + slide out tu phai sang trai (thu ve) khi hover ra.
    /// </summary>
    public class UISidebarTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Fields
        [Header("References")]
        [Tooltip("RectTransform cua tooltip can hien thi.")]
        [SerializeField] private RectTransform _tooltip;
        [Tooltip("CanvasGroup cua tooltip de dieu khien fade in / fade out.")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Animation Settings")]
        [Tooltip("Khoang cach truot (pixel) khi slide in tu trai va slide out sang phai.")]
        [SerializeField] private float _slideDistance = 30f;
        [Tooltip("Thoi gian slide in va fade in (giay).")]
        [SerializeField] private float _fadeInDuration = 0.25f;
        [Tooltip("Ease type khi slide in.")]
        [SerializeField] private Ease _fadeInEase = Ease.OutQuad;
        [Tooltip("Thoi gian fade out va slide out (giay).")]
        [SerializeField] private float _fadeOutDuration = 0.2f;
        [Tooltip("Ease type khi slide out.")]
        [SerializeField] private Ease _fadeOutEase = Ease.InQuad;

        [Header("Audio Settings")]
        [Tooltip("SFX phat khi hover vao nut (tuy chon).")]
        [SerializeField] private AudioClip _hoverSfx;

        private Vector2 _originalPosition;
        private Tween _moveTween;
        private Tween _fadeTween;
        #endregion

        #region Properties
        /// <summary>
        /// RectTransform cua tooltip.
        /// </summary>
        public RectTransform Tooltip => _tooltip;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_tooltip == null)
            {
                // Tu tim child dau tien neu chua keo tha vao Inspector
                _tooltip = transform.Find("Tooltip") as RectTransform;
            }

            if (_tooltip != null)
            {
                _originalPosition = _tooltip.anchoredPosition;

                if (_canvasGroup == null)
                {
                    _canvasGroup = _tooltip.GetComponent<CanvasGroup>();
                    if (_canvasGroup == null)
                    {
                        _canvasGroup = _tooltip.gameObject.AddComponent<CanvasGroup>();
                    }
                }

                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;

                _tooltip.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            KillTweens();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            if (_tooltip != null)
            {
                _tooltip.anchoredPosition = _originalPosition;
                _tooltip.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            KillTweens();
        }
        #endregion

        #region Pointer Events
        public void OnPointerEnter(PointerEventData eventData)
        {
            Show();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Hien thi tooltip voi hieu ung slide in tu trai sang + fade in.
        /// </summary>
        public void Show()
        {
            if (_tooltip == null || _canvasGroup == null) return;

            KillTweens();

            if (_hoverSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_hoverSfx);
            }

            _tooltip.gameObject.SetActive(true);

            // Dat vi tri bat dau lui sang trai neu dang an
            if (_canvasGroup.alpha <= 0.05f)
            {
                _tooltip.anchoredPosition = _originalPosition - new Vector2(_slideDistance, 0f);
            }

            _fadeTween = _canvasGroup.DOFade(1f, _fadeInDuration).SetEase(_fadeInEase).SetUpdate(true);
            _moveTween = _tooltip.DOAnchorPos(_originalPosition, _fadeInDuration).SetEase(_fadeInEase).SetUpdate(true);
        }

        /// <summary>
        /// An tooltip voi hieu ung fade out + slide out tu phai sang trai.
        /// </summary>
        public void Hide()
        {
            if (_tooltip == null || _canvasGroup == null) return;

            KillTweens();

            Vector2 exitPos = _originalPosition - new Vector2(_slideDistance, 0f);

            _fadeTween = _canvasGroup.DOFade(0f, _fadeOutDuration).SetEase(_fadeOutEase).SetUpdate(true);
            _moveTween = _tooltip.DOAnchorPos(exitPos, _fadeOutDuration).SetEase(_fadeOutEase).SetUpdate(true)
                .OnComplete(() =>
                {
                    if (_tooltip != null)
                    {
                        _tooltip.gameObject.SetActive(false);
                        _tooltip.anchoredPosition = _originalPosition;
                    }
                });
        }
        #endregion

        #region Private Methods
        private void KillTweens()
        {
            _moveTween?.Kill();
            _fadeTween?.Kill();
        }
        #endregion
    }
}
