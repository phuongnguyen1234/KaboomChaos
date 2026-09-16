using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Core;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Component dieu khien hieu ung Target Lock trong khong gian the gioi (World Space).
    /// Tu dong di chuyen theo player, billboard (xoay mat ve Camera chinh) va tu dong
    /// tra ve pool khi hoan tat animation scale up + fade in/out.
    /// </summary>
    public class WorldTargetLockController : MonoBehaviour
    {
        #region Fields

        [Header("Target Lock Settings")]
        [Tooltip("Offset chieu cao phia tren dau player.")]
        [SerializeField] private float _heightOffset = 1.5f;

        [Header("Animation Settings")]
        [Tooltip("Thoi gian scale up va fade in (giay).")]
        [SerializeField] private float _scaleUpDuration = 0.25f;

        [Tooltip("Thoi gian tam dung hien thi (giay).")]
        [SerializeField] private float _holdDuration = 0.15f;

        [Tooltip("Thoi gian fade out va bien mat (giay).")]
        [SerializeField] private float _fadeOutDuration = 0.35f;

        [Header("SFX")]
        [Tooltip("SFX phat khi hieu ung target lock xuat hien.")]
        [SerializeField] private AudioClip _targetLockSfx;

        // Cached components
        private Transform _targetPlayer;
        private Camera _mainCamera;
        private Sequence _activeSequence;
        private CanvasGroup _canvasGroup;
        private SpriteRenderer _spriteRenderer;
        private MaterialEffectController _materialEffectController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _canvasGroup = GetComponentInChildren<CanvasGroup>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _materialEffectController = GetComponentInChildren<MaterialEffectController>();
        }

        private void OnDisable()
        {
            _activeSequence?.Kill();
            transform.DOKill();
            _targetPlayer = null;
        }

        private void LateUpdate()
        {
            BillboardAndFollow();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Kich hoat hieu ung Target Lock tren mot player cu the.
        /// </summary>
        /// <param name="targetPlayer">Transform cua player bi nham muc tieu.</param>
        /// <param name="customHeightOffset">Offset chieu cao tuy chon (neu khong truyen se dung gia tri _heightOffset trong Inspector).</param>
        public void Trigger(Transform targetPlayer, float? customHeightOffset = null)
        {
            _targetPlayer = targetPlayer;
            if (customHeightOffset.HasValue)
            {
                _heightOffset = customHeightOffset.Value;
            }

            _activeSequence?.Kill();
            transform.DOKill();

            if (_mainCamera == null) _mainCamera = Camera.main;

            BillboardAndFollow();

            // Setup trang thai ban dau
            transform.localScale = Vector3.one * 0.4f;
            SetAlpha(0f);

            if (_targetLockSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_targetLockSfx, transform.position);
            }

            _activeSequence = DOTween.Sequence();
            _activeSequence.SetUpdate(UpdateType.Late, true);
            _activeSequence.SetAutoKill(true);

            // Timeline: Scale up + Fade in -> Hold -> Scale up & Fade out -> Despawn
            _activeSequence.Append(transform.DOScale(Vector3.one, _scaleUpDuration).SetEase(Ease.OutBack));
            _activeSequence.Join(DOTween.To(() => GetAlpha(), x => SetAlpha(x), 1f, _scaleUpDuration * 0.8f).SetEase(Ease.OutQuad));
            _activeSequence.AppendInterval(_holdDuration);
            _activeSequence.Append(DOTween.To(() => GetAlpha(), x => SetAlpha(x), 0f, _fadeOutDuration).SetEase(Ease.InQuad));
            _activeSequence.Join(transform.DOScale(Vector3.one * 1.3f, _fadeOutDuration).SetEase(Ease.InQuad));

            _activeSequence.OnComplete(DespawnSelf);
        }

        #endregion

        #region Private Methods

        private void BillboardAndFollow()
        {
            if (_targetPlayer != null && _targetPlayer.gameObject != null && _targetPlayer.gameObject.activeInHierarchy)
            {
                transform.position = _targetPlayer.position + Vector3.up * _heightOffset;
            }

            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                transform.rotation = _mainCamera.transform.rotation;
            }
        }

        private float GetAlpha()
        {
            if (_canvasGroup != null) return _canvasGroup.alpha;
            if (_spriteRenderer != null) return _spriteRenderer.color.a;
            if (_materialEffectController != null) return _materialEffectController.CustomAlpha;
            return 1f;
        }

        private void SetAlpha(float alpha)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = alpha;
            if (_spriteRenderer != null)
            {
                Color c = _spriteRenderer.color;
                c.a = alpha;
                _spriteRenderer.color = c;
            }
            if (_materialEffectController != null) _materialEffectController.CustomAlpha = alpha;
        }

        private void DespawnSelf()
        {
            if (GameEvents.IsVFXPoolListening())
            {
                GameEvents.TriggerVFXDespawnRequest(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion
    }
}

