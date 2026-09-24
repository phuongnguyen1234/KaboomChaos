using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Loai hieu ung hover tren button UI (Scale, Doi mau hoac ca hai).
    /// </summary>
    public enum HoverEffectType
    {
        Scale,
        Color,
        ScaleAndColor
    }

    /// <summary>
    /// Component ho tro hieu ung hover (scale len, doi mau) va phat am thanh khi click cho button tren UI.
    /// </summary>
    public class UIButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        #region Fields

        [Header("Hover Type")]
        [Tooltip("Kieu hieu ung hover: Scale, Color hoac ScaleAndColor.")]
        [SerializeField] private HoverEffectType _effectType = HoverEffectType.Scale;

        [Header("Scale Settings")]
        [SerializeField] private float _hoverScale = 1.1f;
        [SerializeField] private float _animationDuration = 0.2f;
        [SerializeField] private Ease _easeType = Ease.OutBack;

        [Header("Color Settings")]
        [Tooltip("Graphic se doi mau khi hover (Image, TextMeshProUGUI...). Neu de trong, tu tim Graphic tren GameObject.")]
        [SerializeField] private Graphic _targetGraphic;
        [Tooltip("Mau phat sang/thay doi khi con tro chuot di chuyen vao button.")]
        [SerializeField] private Color _hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _clickSound;
        [SerializeField] private AudioClip _hoverSound;

        private Vector3 _originalScale;
        private Color _originalColor;
        private Tween _scaleTween;
        private Tween _colorTween;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _originalScale = transform.localScale;

            if (_targetGraphic == null)
            {
                _targetGraphic = GetComponent<Graphic>();
            }

            if (_targetGraphic != null)
            {
                _originalColor = _targetGraphic.color;
            }
        }

        private void OnDisable()
        {
            // Reset scale va color khi bi disable de tranh loi hien thi khi enable lai
            _scaleTween?.Kill();
            _colorTween?.Kill();

            transform.localScale = _originalScale;
            if (_targetGraphic != null)
            {
                _targetGraphic.color = _originalColor;
            }
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
            _colorTween?.Kill();
        }

        #endregion

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_hoverSound != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_hoverSound);
            }

            // Hieu ung Scale
            if (_effectType == HoverEffectType.Scale || _effectType == HoverEffectType.ScaleAndColor)
            {
                _scaleTween?.Kill();
                // SetUpdate(true) de animation van chay ke ca khi game tam dung (Time.timeScale = 0)
                _scaleTween = transform.DOScale(_originalScale * _hoverScale, _animationDuration).SetEase(_easeType).SetUpdate(true);
            }

            // Hieu ung Color
            if ((_effectType == HoverEffectType.Color || _effectType == HoverEffectType.ScaleAndColor) && _targetGraphic != null)
            {
                _colorTween?.Kill();
                _colorTween = _targetGraphic.DOColor(_hoverColor, _animationDuration).SetUpdate(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Hieu ung Scale
            if (_effectType == HoverEffectType.Scale || _effectType == HoverEffectType.ScaleAndColor)
            {
                _scaleTween?.Kill();
                _scaleTween = transform.DOScale(_originalScale, _animationDuration).SetEase(_easeType).SetUpdate(true);
            }

            // Hieu ung Color
            if ((_effectType == HoverEffectType.Color || _effectType == HoverEffectType.ScaleAndColor) && _targetGraphic != null)
            {
                _colorTween?.Kill();
                _colorTween = _targetGraphic.DOColor(_originalColor, _animationDuration).SetUpdate(true);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_clickSound != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_clickSound);
            }

            // Hieu ung pop nhe khi click de tao cam giac nhan phim
            if (_effectType == HoverEffectType.Scale || _effectType == HoverEffectType.ScaleAndColor)
            {
                _scaleTween?.Kill();
                transform.localScale = _originalScale * (_hoverScale * 0.9f);
                _scaleTween = transform.DOScale(_originalScale * _hoverScale, _animationDuration / 2f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        #endregion
    }
}

