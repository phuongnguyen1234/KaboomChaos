using System;
using UnityEngine;
using DG.Tweening;

namespace UI.Animations
{
    /// <summary>
    /// Animation chuyen canh (Transition Screen) khi bat dau va ket thuc round.
    /// Chu ky: Scale up logo -> Tang size CircleMask len 3000 -> Callback che man hinh -> Giam size CircleMask ve 0 -> Logo scale down ve 0.
    /// </summary>
    public class TransitionScreenAnimation : MonoBehaviour
    {
        #region Fields
        [Header("UI Elements")]
        [Tooltip("RectTransform cua Logo.")]
        [SerializeField] private RectTransform _logo;

        [Tooltip("RectTransform cua CircleMask (object hinh tron che/mo man hinh).")]
        [SerializeField] private RectTransform _circleMask;

        [Header("Animation Settings")]
        [Tooltip("Thoi gian scale logo.")]
        [SerializeField] private float _logoScaleDuration = 0.4f;

        [Tooltip("Thoi gian thay doi size cua CircleMask.")]
        [SerializeField] private float _maskSizeDuration = 0.5f;

        [Tooltip("Thoi gian cho giu man hinh che kin truoc khi CircleMask scale down.")]
        [SerializeField] private float _holdCoveredDuration = 0.1f;

        [Tooltip("Kich thuoc 2 chieu toi da cua CircleMask khi che phu man hinh.")]
        [SerializeField] private float _targetMaskSize = 3000f;

        [Tooltip("Ease type cho logo scale up.")]
        [SerializeField] private Ease _logoInEase = Ease.OutBack;

        [Tooltip("Ease type cho logo scale down.")]
        [SerializeField] private Ease _logoOutEase = Ease.InBack;

        [Tooltip("Ease type cho CircleMask.")]
        [SerializeField] private Ease _maskEase = Ease.InOutQuad;

        private Sequence _activeSequence;
        private Action _onCoveredCallbacks;
        private Action _onCompleteCallbacks;
        private bool _isCovered = false;
        #endregion

        #region Properties
        /// <summary>
        /// Thoi gian giu man hinh che kin truoc khi CircleMask scale down.
        /// </summary>
        public float HoldCoveredDuration
        {
            get => _holdCoveredDuration;
            set => _holdCoveredDuration = Mathf.Max(0f, value);
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            ResetVisuals();
        }

        private void OnDisable()
        {
            ResetVisuals();
        }

        private void OnDestroy()
        {
            ResetVisuals();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Dat cac thanh phan ve trang thai ban dau va goi cac callback dang cho (neu co).
        /// </summary>
        public void ResetVisuals()
        {
            if (_activeSequence != null)
            {
                _activeSequence.Kill();
                _activeSequence = null;
            }

            var pendingCovered = _onCoveredCallbacks;
            var pendingComplete = _onCompleteCallbacks;
            _onCoveredCallbacks = null;
            _onCompleteCallbacks = null;
            _isCovered = false;

            // Goi callback dang cho de khong treo coroutine caller
            pendingCovered?.Invoke();
            pendingComplete?.Invoke();

            if (_logo != null) _logo.localScale = Vector3.zero;
            if (_circleMask != null)
            {
                _circleMask.anchoredPosition = Vector2.zero;
                _circleMask.sizeDelta = Vector2.zero;
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Chay hieu ung transition chuyen canh. Neu dang co transition chay, gop callback vao sequence hien tai.
        /// </summary>
        /// <param name="onCovered">Callback duoc goi khi CircleMask da phong to 3000 (man hinh da che hoan toan).</param>
        /// <param name="onComplete">Callback duoc goi khi qua trinh transition hoan tat.</param>
        /// <param name="holdDurationOverride">Thoi gian giu man hinh che kin tuy chon (neu null se dung _holdCoveredDuration).</param>
        public void PlayTransition(Action onCovered = null, Action onComplete = null, float? holdDurationOverride = null)
        {
            // Neu sequence dang chay, gop callback thay vi kill lam treo caller khac
            if (_activeSequence != null && _activeSequence.IsActive() && _activeSequence.IsPlaying())
            {
                if (onCovered != null)
                {
                    if (_isCovered)
                    {
                        onCovered.Invoke();
                    }
                    else
                    {
                        _onCoveredCallbacks += onCovered;
                    }
                }

                if (onComplete != null)
                {
                    _onCompleteCallbacks += onComplete;
                }
                return;
            }

            ResetVisuals();
            gameObject.SetActive(true);

            _isCovered = false;
            _onCoveredCallbacks = onCovered;
            _onCompleteCallbacks = onComplete;

            _activeSequence = DOTween.Sequence().SetUpdate(true);

            // 1. Scale up logo dong thoi tang size CircleMask den targetMaskSize
            if (_logo != null)
            {
                _logo.localScale = Vector3.zero;
                _activeSequence.Append(_logo.DOScale(Vector3.one, _logoScaleDuration).SetEase(_logoInEase));
            }

            if (_circleMask != null)
            {
                _circleMask.anchoredPosition = Vector2.zero;
                _circleMask.sizeDelta = Vector2.zero;
                if (_logo != null)
                {
                    _activeSequence.Join(_circleMask.DOSizeDelta(new Vector2(_targetMaskSize, _targetMaskSize), _maskSizeDuration).SetEase(_maskEase));
                }
                else
                {
                    _activeSequence.Append(_circleMask.DOSizeDelta(new Vector2(_targetMaskSize, _targetMaskSize), _maskSizeDuration).SetEase(_maskEase));
                }
            }

            // 2. Noi dung chuyen canh (Teleport player / Clean up map) khi man hinh da che
            _activeSequence.AppendCallback(() =>
            {
                _isCovered = true;
                var coveredCb = _onCoveredCallbacks;
                _onCoveredCallbacks = null;
                coveredCb?.Invoke();
            });

            // Tam hoan giu man hinh che kin theo thoi gian cau hinh
            float holdDuration = holdDurationOverride ?? _holdCoveredDuration;
            if (holdDuration > 0f)
            {
                _activeSequence.AppendInterval(holdDuration);
            }

            // 3. Giam size CircleMask ve 0 dong thoi Logo scale down ve 0
            if (_circleMask != null)
            {
                _activeSequence.Append(_circleMask.DOSizeDelta(Vector2.zero, _maskSizeDuration).SetEase(_maskEase));
            }

            if (_logo != null)
            {
                if (_circleMask != null)
                {
                    _activeSequence.Join(_logo.DOScale(Vector3.zero, _logoScaleDuration).SetEase(_logoOutEase));
                }
                else
                {
                    _activeSequence.Append(_logo.DOScale(Vector3.zero, _logoScaleDuration).SetEase(_logoOutEase));
                }
            }

            // Hoan tat
            _activeSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
                var completeCb = _onCompleteCallbacks;
                _onCompleteCallbacks = null;
                _activeSequence = null;
                completeCb?.Invoke();
            });
        }
        #endregion
    }
}

