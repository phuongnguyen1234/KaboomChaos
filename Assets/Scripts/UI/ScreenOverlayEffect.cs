using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Core;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Quan ly cac hieu ung overlay tren man hinh nguoi choi (damage flash, freeze overlay).
    /// </summary>
    public class ScreenOverlayEffect : MonoBehaviour
    {
        #region Fields

        [Header("Damage Overlay Settings")]
        [Tooltip("CanvasGroup cua damage overlay image.")]
        [SerializeField] private CanvasGroup _damageOverlayCanvasGroup;

        [Tooltip("RectTransform cua damage overlay de thuc hien animation scale.")]
        [SerializeField] private RectTransform _damageOverlayRect;

        [Tooltip("Do alpha toi da khi nhan sat thuong.")]
        [Range(0f, 1f)]
        [SerializeField] private float _damageMaxAlpha = 0.8f;

        [Tooltip("Scale ban dau khi bat dau nhan sat thuong (Giai doan 1: scale down).")]
        [SerializeField] private float _damageStartScale = 1.2f;

        [Tooltip("Scale trung gian o dinh diem nhan sat thuong.")]
        [SerializeField] private float _damagePeakScale = 1.0f;

        [Tooltip("Scale ket thuc khi mo dan (Giai doan 2: scale up).")]
        [SerializeField] private float _damageEndScale = 1.2f;

        [Tooltip("Thoi gian fade in va scale down (giay).")]
        [SerializeField] private float _damageFadeInDuration = 0.15f;

        [Tooltip("Thoi gian fade out va scale up (giay).")]
        [SerializeField] private float _damageFadeOutDuration = 0.25f;

        [Header("Freeze Overlay Settings")]
        [Tooltip("CanvasGroup cua freeze overlay image.")]
        [SerializeField] private CanvasGroup _freezeOverlayCanvasGroup;

        [Tooltip("Thoi gian fade in/out cua hieu ung dong bang (giay).")]
        [SerializeField] private float _freezeFadeDuration = 0.3f;

        [Header("Explosion Overlay Settings")]
        [Tooltip("CanvasGroup cua explosion overlay image (neu khong gan se tu dong dung damage overlay).")]
        [SerializeField] private CanvasGroup _explosionOverlayCanvasGroup;

        [Tooltip("RectTransform cua explosion overlay de thuc hien animation scale (neu khong gan se tu dong dung damage overlay).")]
        [SerializeField] private RectTransform _explosionOverlayRect;

        [Tooltip("Do alpha toi da cua hieu ung overlay vu no khi o rat gan.")]
        [Range(0f, 1f)]
        [SerializeField] private float _explosionMaxAlpha = 0.7f;

        [Tooltip("Scale ban dau khi bat dau vu no.")]
        [SerializeField] private float _explosionStartScale = 1.0f;

        [Tooltip("Scale trung gian o dinh diem hieu ung vu no.")]
        [SerializeField] private float _explosionPeakScale = 1.15f;

        [Tooltip("Scale ket thuc khi mo dan (Giai doan 2: scale up).")]
        [SerializeField] private float _explosionEndScale = 1.35f;

        [Tooltip("Thoi gian fade in (giay).")]
        [SerializeField] private float _explosionFadeInDuration = 0.10f;

        [Tooltip("Thoi gian fade out va scale up (giay).")]
        [SerializeField] private float _explosionFadeOutDuration = 0.35f;

        private Sequence _damageSequence;
        private Sequence _explosionSequence;
        private Tween _freezeTween;
        private bool _isFrozen;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeOverlayStates();
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDamageTaken += HandlePlayerDamageTaken;
            GameEvents.OnPlayerStatusEffectApplied += HandlePlayerStatusEffectApplied;
            GameEvents.OnPlayerStatusEffectReverted += HandlePlayerStatusEffectReverted;
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnRoundEndPlayerReset += HandleResetOverlayState;
            GameEvents.OnReturnToHomeRequest += HandleResetOverlayState;
            GameEvents.OnExplosionOccurred += HandleExplosionOccurred;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDamageTaken -= HandlePlayerDamageTaken;
            GameEvents.OnPlayerStatusEffectApplied -= HandlePlayerStatusEffectApplied;
            GameEvents.OnPlayerStatusEffectReverted -= HandlePlayerStatusEffectReverted;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnRoundEndPlayerReset -= HandleResetOverlayState;
            GameEvents.OnReturnToHomeRequest -= HandleResetOverlayState;
            GameEvents.OnExplosionOccurred -= HandleExplosionOccurred;

            KillAllTweens();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Kich hoat hieu ung damage overlay thu cong.
        /// </summary>
        public void PlayDamageEffect()
        {
            if (_damageOverlayCanvasGroup == null || _damageOverlayRect == null) return;

            // Huy sequence cu neu dang chay de reset mượt ma khi nhan sat thuong lien tiep
            _damageSequence?.Kill();

            _damageOverlayCanvasGroup.alpha = 0f;
            _damageOverlayRect.localScale = Vector3.one * _damageStartScale;

            _damageSequence = DOTween.Sequence();

            // Giai doan 1: Scale down va Fade in
            _damageSequence.Append(_damageOverlayCanvasGroup.DOFade(_damageMaxAlpha, _damageFadeInDuration).SetEase(Ease.OutQuad));
            _damageSequence.Join(_damageOverlayRect.DOScale(Vector3.one * _damagePeakScale, _damageFadeInDuration).SetEase(Ease.OutQuad));

            // Giai doan 2: Scale up va Fade out
            _damageSequence.Append(_damageOverlayCanvasGroup.DOFade(0f, _damageFadeOutDuration).SetEase(Ease.InQuad));
            _damageSequence.Join(_damageOverlayRect.DOScale(Vector3.one * _damageEndScale, _damageFadeOutDuration).SetEase(Ease.InQuad));
        }

        /// <summary>
        /// Kich hoat hoac an hieu ung dong bang thu cong.
        /// </summary>
        /// <param name="frozen">True de hien overlay dong bang va muffle BGM, false de an.</param>
        public void SetFreezeEffect(bool frozen)
        {
            if (_freezeOverlayCanvasGroup == null) return;

            _isFrozen = frozen;
            _freezeTween?.Kill();

            float targetAlpha = frozen ? 1f : 0f;
            _freezeTween = _freezeOverlayCanvasGroup.DOFade(targetAlpha, _freezeFadeDuration)
                .SetEase(frozen ? Ease.OutQuad : Ease.InQuad);

            // Yeu cau muffle hoac unmuffle BGM qua GameEvents
            GameEvents.TriggerBgmAudioMuffleRequested(frozen);
        }

        /// <summary>
        /// Kich hoat hieu ung overlay vu no voi cuong do intensity phu thuoc khoang cach.
        /// </summary>
        /// <param name="intensity">Cuong do tu 0.0 toi 1.0.</param>
        public void PlayExplosionEffect(float intensity)
        {
            var canvasGroup = GetExplosionCanvasGroup();
            var rect = GetExplosionRectTransform();

            if (canvasGroup == null || rect == null) return;

            _explosionSequence?.Kill();

            float targetAlpha = _explosionMaxAlpha * Mathf.Clamp01(intensity);
            canvasGroup.alpha = 0f;
            rect.localScale = Vector3.one * _explosionStartScale;

            _explosionSequence = DOTween.Sequence();

            // Giai doan 1: Scale va Fade in nhẹ
            _explosionSequence.Append(canvasGroup.DOFade(targetAlpha, _explosionFadeInDuration).SetEase(Ease.OutQuad));
            _explosionSequence.Join(rect.DOScale(Vector3.one * _explosionPeakScale, _explosionFadeInDuration).SetEase(Ease.OutQuad));

            // Giai doan 2: Scale up va Fade out (giong damage effect)
            _explosionSequence.Append(canvasGroup.DOFade(0f, _explosionFadeOutDuration).SetEase(Ease.InQuad));
            _explosionSequence.Join(rect.DOScale(Vector3.one * _explosionEndScale, _explosionFadeOutDuration).SetEase(Ease.InQuad));
        }

        #endregion

        #region Private Methods

        private CanvasGroup GetExplosionCanvasGroup() => _explosionOverlayCanvasGroup != null ? _explosionOverlayCanvasGroup : _damageOverlayCanvasGroup;
        private RectTransform GetExplosionRectTransform() => _explosionOverlayRect != null ? _explosionOverlayRect : _damageOverlayRect;

        /// <summary>
        /// Khoi tao trang thai ban dau cua cac overlay (alpha = 0).
        /// </summary>
        private void InitializeOverlayStates()
        {
            if (_damageOverlayCanvasGroup != null)
            {
                _damageOverlayCanvasGroup.alpha = 0f;
                _damageOverlayCanvasGroup.blocksRaycasts = false;
                _damageOverlayCanvasGroup.interactable = false;
            }

            if (_freezeOverlayCanvasGroup != null)
            {
                _freezeOverlayCanvasGroup.alpha = 0f;
                _freezeOverlayCanvasGroup.blocksRaycasts = false;
                _freezeOverlayCanvasGroup.interactable = false;
            }

            var explosionGroup = GetExplosionCanvasGroup();
            if (explosionGroup != null && explosionGroup != _damageOverlayCanvasGroup)
            {
                explosionGroup.alpha = 0f;
                explosionGroup.blocksRaycasts = false;
                explosionGroup.interactable = false;
            }

            if (_damageOverlayRect != null)
            {
                _damageOverlayRect.localScale = Vector3.one * _damageStartScale;
            }

            var explosionRect = GetExplosionRectTransform();
            if (explosionRect != null && explosionRect != _damageOverlayRect)
            {
                explosionRect.localScale = Vector3.one * _explosionStartScale;
            }
        }

        /// <summary>
        /// Kiem tra xem player duoc truyen vao co phai la player cuc bo (current player) hay khong.
        /// </summary>
        private bool IsCurrentPlayer(IPlayer player)
        {
            if (player == null) return false;
            IPlayer currentPlayer = IPlayerManager.Instance?.GetCurrentPlayer();
            return currentPlayer != null && currentPlayer == player;
        }

        /// <summary>
        /// Xu ly event khi player nhan sat thuong.
        /// </summary>
        private void HandlePlayerDamageTaken(IPlayer player, float amount, DamageSourceType sourceType, StatusEffectType effectContext)
        {
            if (!IsCurrentPlayer(player)) return;

            PlayDamageEffect();
        }

        /// <summary>
        /// Xu ly event khi player duoc ap dung hieu ung trang thai (check Frozen).
        /// </summary>
        private void HandlePlayerStatusEffectApplied(IPlayer player, StatusEffectType effect)
        {
            if (!IsCurrentPlayer(player)) return;

            if (effect == StatusEffectType.Frozen)
            {
                SetFreezeEffect(true);
            }
        }

        /// <summary>
        /// Xu ly event khi hieu ung trang thai cua player duoc hoan tac (check Frozen).
        /// </summary>
        private void HandlePlayerStatusEffectReverted(IPlayer player, StatusEffectType effect)
        {
            if (!IsCurrentPlayer(player)) return;

            if (effect == StatusEffectType.Frozen)
            {
                SetFreezeEffect(false);
            }
        }

        /// <summary>
        /// Xu ly event khi co vu no xay ra trong game (hien thi scale up fade overlay khi o gan).
        /// </summary>
        private void HandleExplosionOccurred(Vector3 explosionCenter, float radius)
        {
            var settings = Core.Interfaces.SettingsService.Instance;
            if (settings != null && !settings.ScreenShakeEnabled)
            {
                return;
            }

            IPlayer currentPlayer = IPlayerManager.Instance?.GetCurrentPlayer();
            Vector3 playerPos;

            if (currentPlayer != null && currentPlayer.GameObject != null)
            {
                playerPos = currentPlayer.GameObject.transform.position;
            }
            else if (Camera.main != null)
            {
                playerPos = Camera.main.transform.position;
            }
            else
            {
                return;
            }

            float distance = Vector3.Distance(explosionCenter, playerPos);
            float maxDistance = Mathf.Max(radius * 3f, 18f);

            if (distance < maxDistance)
            {
                float intensity = Mathf.Clamp01(1f - (distance / maxDistance));
                PlayExplosionEffect(intensity);
            }
        }

        /// <summary>
        /// Xu ly event khi player chet -> an ngay freeze overlay va unmuffle BGM.
        /// </summary>
        private void HandlePlayerDied(IPlayer player)
        {
            if (!IsCurrentPlayer(player)) return;

            ResetOverlayState();
        }

        /// <summary>
        /// Reset lai tat ca cac overlay ve trang thai ban dau.
        /// </summary>
        private void HandleResetOverlayState()
        {
            ResetOverlayState();
        }

        /// <summary>
        /// An tat ca overlay va unmuffle BGM.
        /// </summary>
        private void ResetOverlayState()
        {
            if (_isFrozen)
            {
                SetFreezeEffect(false);
            }

            _damageSequence?.Kill();
            if (_damageOverlayCanvasGroup != null)
            {
                _damageOverlayCanvasGroup.alpha = 0f;
            }

            _explosionSequence?.Kill();
            var explosionGroup = GetExplosionCanvasGroup();
            if (explosionGroup != null && explosionGroup != _damageOverlayCanvasGroup)
            {
                explosionGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Huy tat ca tween khi component bi disable.
        /// </summary>
        private void KillAllTweens()
        {
            _damageSequence?.Kill();
            _explosionSequence?.Kill();
            _freezeTween?.Kill();
        }

        #endregion
    }
}

