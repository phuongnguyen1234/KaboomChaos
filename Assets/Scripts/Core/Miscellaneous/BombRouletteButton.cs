using UnityEngine;
using DG.Tweening;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Nut bam trong minigame Bomb Roulette trong Lobby.
    /// Khi Player giam len nut, nut se lun xuong, tat emission intensity ve 0,
    /// phat VFX/SFX va thong bao ve cho BombRouletteMinigame xu ly logic.
    /// </summary>
    public class BombRouletteButton : MonoBehaviour
    {
        #region Fields

        [Header("Sink Settings")]
        [Tooltip("Do sau lun xuong (don vi local Y) khi player giam len nut.")]
        [SerializeField] private float _sinkDepth = 0.2f;

        [Tooltip("Thoi gian (giay) chay animation lun xuong cua nut.")]
        [SerializeField] private float _sinkDuration = 0.15f;

        [Header("Material Effect Controller")]
        [Tooltip("Tham chieu den MaterialEffectController. Neu de trong, script tu dong tim tren object.")]
        [SerializeField] private MaterialEffectController _effectController;

        [Header("Audio & Visual Effects")]
        [Tooltip("SFX phat khi player giam len nut.")]
        [SerializeField] private AudioClip _stepSfx;

        [Tooltip("VFX prefab tao ra tai vi tri nut khi giam (tuy chon).")]
        [SerializeField] private GameObject _stepVfxPrefab;

        // Components va trang thai runtime
        private Vector3 _initialLocalPos;
        private float _initialEmissionIntensity = 1f;
        private bool _isPressed;
        private BombRouletteMinigame _minigameController;
        private Tweener _sinkTween;

        #endregion

        #region Properties

        /// <summary>
        /// Cho biet nut da bi giam xuong trong luot choi hien tai hay chua.
        /// </summary>
        public bool IsPressed => _isPressed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _initialLocalPos = transform.localPosition;

            if (_effectController == null)
            {
                _effectController = GetComponent<MaterialEffectController>();
                if (_effectController == null)
                {
                    _effectController = GetComponentInChildren<MaterialEffectController>();
                }
            }

            if (_effectController != null)
            {
                _initialEmissionIntensity = _effectController.CustomEmissionIntensity;
            }
        }

        private void OnDisable()
        {
            _sinkTween?.Kill();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isPressed || _minigameController == null) return;

            // Kiem tra xem collider va cham co thuoc ve than chinh Player (PrimaryTarget) hay khong
            var primaryTarget = other.GetComponentInParent<IPrimaryExplosionTarget>();
            if (primaryTarget == null) return;

            IPlayer player = other.GetComponentInParent<IPlayer>();

            // Danh dau nut da duoc bam
            _isPressed = true;

            // 1. Phat SFX & VFX khi giam
            if (_stepSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_stepSfx, transform.position);
            }

            if (_stepVfxPrefab != null)
            {
                GameEvents.TriggerVFXSpawnRequest(_stepVfxPrefab, transform.position, Quaternion.identity);
            }

            // 2. Chay animation lun xuong
            _sinkTween?.Kill();
            _sinkTween = transform.DOLocalMoveY(_initialLocalPos.y - _sinkDepth, _sinkDuration);

            // 3. Dat emission intensity = 0 qua MaterialEffectController
            if (_effectController != null)
            {
                _effectController.CustomEmissionIntensity = 0f;
            }

            // 4. Thong bao cho master minigame controller
            _minigameController.OnButtonStepped(this, player);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Khoi tao tham chieu den master minigame controller.
        /// </summary>
        public void Initialize(BombRouletteMinigame minigameController)
        {
            _minigameController = minigameController;
        }

        /// <summary>
        /// Reset nut ve vi tri nang len ban dau va khoi phục cuong do phat sang emission.
        /// </summary>
        public void ResetButton()
        {
            _isPressed = false;
            _sinkTween?.Kill();

            _sinkTween = transform.DOLocalMoveY(_initialLocalPos.y, _sinkDuration);

            if (_effectController != null)
            {
                _effectController.CustomEmissionIntensity = _initialEmissionIntensity;
            }
        }

        #endregion
    }
}

